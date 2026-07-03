using System;
using System.Threading.Tasks;
using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.EntityFramework.Repositories;
using Arora.Workflow.EntityFramework.Context;
using Arora.Workflow.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arora.Workflow.IntegrationTests.Repositories;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyAroraWorkflowMappings();
        base.OnModelCreating(modelBuilder);
    }
}

public class EfCoreApprovalRepositoryTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TestDbContext _dbContext;

    public EfCoreApprovalRepositoryTests()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            
        services.AddAroraWorkflow()
            .UseEntityFramework<TestDbContext>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task AddAndGetById_SavesAndRetrievesApproval()
    {
        // Arrange
        var repo = _serviceProvider.GetRequiredService<IApprovalRepository>();
        var tenantId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var actor = new ActorInfo("user123", "John Doe");
        
        var approval = Approval.Create(
            tenantId: tenantId,
            workflowInstanceId: instanceId,
            workflowName: "test-wf",
            correlationId: "corr-1",
            stepName: "ManagerApproval",
            assignedActor: actor,
            createdAt: DateTimeOffset.UtcNow
        );

        // Act
        await repo.AddAsync(approval);
        await _serviceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        var retrieved = await repo.GetByIdAsync(approval.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(approval.Id, retrieved.Id);
        Assert.Equal("ManagerApproval", retrieved.StepName);
        Assert.Equal("user123", retrieved.AssignedActor.Id);
    }

    [Fact]
    public async Task UpdateAsync_SavesChanges()
    {
        // Arrange
        var repo = _serviceProvider.GetRequiredService<IApprovalRepository>();
        var tenantId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var actor = new ActorInfo("user123", "John Doe");
        var clock = DateTimeOffset.UtcNow;
        
        var approval = Approval.Create(
            tenantId: tenantId,
            workflowInstanceId: instanceId,
            workflowName: "test-wf",
            correlationId: "corr-1",
            stepName: "ManagerApproval",
            assignedActor: actor,
            createdAt: clock
        );

        await repo.AddAsync(approval);
        await _serviceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        // Act
        var retrieved = await repo.GetByIdAsync(approval.Id);
        Assert.NotNull(retrieved);

        retrieved.Approve(new ActorInfo("manager123", "Manager"), "Looks good", clock.AddMinutes(5));
        
        await repo.UpdateAsync(retrieved);
        await _serviceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        // Clear tracking
        _dbContext.ChangeTracker.Clear();

        var updated = await repo.GetByIdAsync(approval.Id);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(ApprovalStatus.Approved, updated.Status);
        Assert.Equal("Looks good", updated.Comment);
        Assert.Equal("manager123", updated.DecidedByActor?.Id);
    }

    [Fact]
    public async Task GetLatestApprovalAsync_ReturnsLatestByCreatedAt()
    {
        // Arrange
        var repo = _serviceProvider.GetRequiredService<IApprovalRepository>();
        var instanceId = Guid.NewGuid();
        var actor = new ActorInfo("user1", "User 1");
        
        var app1 = Approval.Create(Guid.NewGuid(), instanceId, "wf", "c", "Step1", actor, DateTimeOffset.UtcNow.AddHours(-2));
        var app2 = Approval.Create(Guid.NewGuid(), instanceId, "wf", "c", "Step1", actor, DateTimeOffset.UtcNow.AddHours(-1)); // Latest
        var app3 = Approval.Create(Guid.NewGuid(), instanceId, "wf", "c", "Step2", actor, DateTimeOffset.UtcNow); // Different step

        await repo.AddAsync(app1);
        await repo.AddAsync(app2);
        await repo.AddAsync(app3);
        await _serviceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        // Act
        var latest = await repo.GetLatestApprovalAsync(instanceId, "Step1");

        // Assert
        Assert.NotNull(latest);
        Assert.Equal(app2.Id, latest.Id);
    }
}
