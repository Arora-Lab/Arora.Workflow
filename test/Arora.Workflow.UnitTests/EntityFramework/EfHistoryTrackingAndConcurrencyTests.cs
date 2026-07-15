using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Application.Services;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.Events;
using Arora.Workflow.Domain.Exceptions;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.EntityFramework;
using Arora.Workflow.EntityFramework.Context;
using Arora.Workflow.EntityFramework.Entities;
using Arora.Workflow.EntityFramework.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arora.Workflow.UnitTests.EntityFramework;

public sealed class TestTenantContext : ITenantContext
{
    public Guid TenantId { get; set; } = Guid.Parse("99999999-9999-9999-9999-999999999999");
}

public sealed class TestClock : IWorkflowClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}

public class TestDbContext : DbContext, IAroraTenantDbContext
{
    private readonly ITenantContext _tenantContext;

    public TestDbContext(DbContextOptions<TestDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public Guid TenantId => _tenantContext.TenantId;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyAroraWorkflowMappings();
        base.OnModelCreating(builder);
    }
}

public class EfHistoryTrackingAndConcurrencyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _db;
    private readonly TestTenantContext _tenantContext;
    private readonly TestClock _clock;
    private readonly DefaultWorkflowHistoryMetadataSanitizer _sanitizer;
    private readonly EfCoreUnitOfWork _uow;

    public EfHistoryTrackingAndConcurrencyTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _tenantContext = new TestTenantContext();
        _clock = new TestClock();
        _sanitizer = new DefaultWorkflowHistoryMetadataSanitizer();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TestDbContext(options, _tenantContext);
        _db.Database.EnsureCreated();

        var provider = new DbContextProvider(_db);
        _uow = new EfCoreUnitOfWork(provider, _tenantContext, _clock, _sanitizer);
    }

    [Fact]
    public async Task SaveChangesAsync_AllocatesSequentialHistorySequences()
    {
        // Arrange
        var clock = DateTimeOffset.UtcNow;
        var startState = new WorkflowState("StartNode", WorkflowStateType.Initial);
        var instance = WorkflowInstance.Start(
            _tenantContext.TenantId,
            Guid.NewGuid(),
            1,
            "test-workflow",
            "corr-123",
            "idemp-123",
            startState,
            null,
            new ActorInfo("actor-1", "Actor One"),
            clock);

        _db.Set<WorkflowInstance>().Add(instance);

        // Act
        await _uow.SaveChangesAsync();

        // Assert
        Assert.Equal(1, instance.HistorySequence);

        var historyRows = await _db.Set<WorkflowHistoryEntity>()
            .Where(x => x.WorkflowInstanceId == instance.Id)
            .OrderBy(x => x.Sequence)
            .ToListAsync();

        Assert.Single(historyRows);
        Assert.Equal(1, historyRows[0].Sequence);
        Assert.Equal("Started", historyRows[0].EventType);
        Assert.Equal("StartNode", historyRows[0].NodeId);
        Assert.Equal("StartNode", historyRows[0].ToState);
    }

    [Fact]
    public async Task SaveChangesAsync_TranslatesDbUpdateConcurrencyException()
    {
        // Arrange
        var clock = DateTimeOffset.UtcNow;
        var startState = new WorkflowState("StartNode", WorkflowStateType.Initial);
        var instance = WorkflowInstance.Start(
            _tenantContext.TenantId,
            Guid.NewGuid(),
            1,
            "test-workflow",
            "corr-123",
            "idemp-123",
            startState,
            null,
            new ActorInfo("actor-1", "Actor One"),
            clock);

        _db.Set<WorkflowInstance>().Add(instance);
        await _uow.SaveChangesAsync();

        // Simulate concurrent modification by setting the tracked original value of RowVersion to a dummy mismatch value
        _db.Entry(instance).Property("RowVersion").OriginalValue = new byte[] { 0, 0, 0, 0 };

        // Mutate original in memory instance and try saving via original Unit of Work
        instance.TransitionTo(new WorkflowState("DifferentNode", WorkflowStateType.Intermediate), "manual2", clock);

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowConcurrencyException>(() => _uow.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_TranslatesUniqueSequenceViolation()
    {
        // Arrange
        var clock = DateTimeOffset.UtcNow;
        var startState = new WorkflowState("StartNode", WorkflowStateType.Initial);
        var instance = WorkflowInstance.Start(
            _tenantContext.TenantId,
            Guid.NewGuid(),
            1,
            "test-workflow",
            "corr-123",
            "idemp-123",
            startState,
            null,
            new ActorInfo("actor-1", "Actor One"),
            clock);

        _db.Set<WorkflowInstance>().Add(instance);
        await _uow.SaveChangesAsync();

        // Manually insert a duplicate sequence history row
        var duplicateHistory = new WorkflowHistoryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            WorkflowInstanceId = instance.Id,
            EventType = "Duplicate",
            Sequence = 1, // Already allocated to WorkflowStarted
            OccurredAt = DateTimeOffset.UtcNow
        };

        _db.Set<WorkflowHistoryEntity>().Add(duplicateHistory);

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowConcurrencyException>(() => _uow.SaveChangesAsync());
    }

    [Fact]
    public void Sanitizer_FiltersMetadataAccordingToAllowlist()
    {
        // Arrange
        var startedEvent = new WorkflowStarted(
            Guid.NewGuid(),
            "test-workflow",
            1,
            "corr-123",
            new ActorInfo("actor-1", "Actor One"),
            DateTimeOffset.UtcNow,
            "StartNode",
            "StartNode");

        var context = new WorkflowHistoryMetadataContext(
            _tenantContext.TenantId,
            startedEvent.WorkflowInstanceId,
            nameof(WorkflowStarted),
            null);

        // Act
        var result = _sanitizer.Sanitize(startedEvent, context);

        // Assert
        Assert.NotNull(result);
        var json = JsonSerializer.Serialize(result);
        Assert.Contains("test-workflow", json);
        Assert.Contains("corr-123", json);
        Assert.Contains("WorkflowVersion", json);
        
        // Ensure sensitive user/input info is not mapped in allowlist
        Assert.DoesNotContain("InitiatedBy", json);
        Assert.DoesNotContain("actor-1", json);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
