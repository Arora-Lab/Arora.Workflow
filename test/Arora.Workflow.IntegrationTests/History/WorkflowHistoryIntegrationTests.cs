using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.Entities;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.EntityFramework.Context;
using Arora.Workflow.EntityFramework.Interceptors;
using Arora.Workflow.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arora.Workflow.IntegrationTests.History;

public class WorkflowHistoryIntegrationTests : IAsyncLifetime
{
    private class TestDbContext : DbContext
    {
        public DbSet<WorkflowInstance> Instances { get; set; } = null!;
        public DbSet<Arora.Workflow.EntityFramework.Entities.WorkflowHistoryEntity> Histories { get; set; } = null!;

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyAroraWorkflowMappings();
            base.OnModelCreating(modelBuilder);
        }
    }

    private ServiceProvider _serviceProvider = null!;

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AroraWorkflowBuilder).Assembly));

        // Arora Workflow EF Core extensions
        var builder = new AroraWorkflowBuilder(services);
        builder.UseEntityFramework<TestDbContext>();

        // DbContext
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<TestDbContext>((sp, options) =>
        {
            
            options.UseInMemoryDatabase(dbName).AddInterceptors(sp.GetRequiredService<Arora.Workflow.EntityFramework.Interceptors.DomainEventDispatcherInterceptor>())
                   ;
        });

        _serviceProvider = services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return _serviceProvider.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task StartingWorkflow_WritesHistoryRecord()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var historyRepo = scope.ServiceProvider.GetRequiredService<IWorkflowHistoryRepository>();
        var instanceRepo = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();

        var instance = WorkflowInstance.Start(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "HistoryTest",
            "CORR-01",
            "IDEMP-01",
            new WorkflowState("Initial", WorkflowStateType.Initial),
            null,
            new ActorInfo("tester", "Tester"),
            DateTimeOffset.UtcNow);

        // Act
        await instanceRepo.AddAsync(instance);
        await uow.SaveChangesAsync(); // This should trigger interceptor -> mediatR -> handler -> insert history

        // Assert
        var histories = await historyRepo.GetByInstanceIdAsync(instance.Id);
        Assert.Single(histories);
        var record = histories[0];
        Assert.Equal("Started", record.Action);
        Assert.Equal("tester", record.Actor?.Id);
    }
}
