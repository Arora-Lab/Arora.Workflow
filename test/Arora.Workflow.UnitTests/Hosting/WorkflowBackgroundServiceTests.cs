using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Hosting;
using Arora.Workflow.Internal.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arora.Workflow.UnitTests.Hosting;

public class WorkflowBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_PollsAndAdvancesRunningWorkflows()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var mockInstanceRepo = new Mock<IWorkflowInstanceRepository>();
        var mockEngine = new Mock<IWorkflowEngine>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        var instance = WorkflowInstance.Start(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            1, 
            "TestWorkflow", 
            "corr1", 
            "key1", 
            new Arora.Workflow.Domain.ValueObjects.WorkflowState("Step1", Arora.Workflow.Domain.ValueObjects.WorkflowStateType.Initial), 
            "{}", 
            new Arora.Workflow.Domain.ValueObjects.ActorInfo("sys", "System"), 
            DateTimeOffset.UtcNow);

        mockInstanceRepo
            .Setup(x => x.GetRunningInstancesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkflowInstance> { instance });

        services.AddScoped(_ => mockInstanceRepo.Object);
        services.AddScoped(_ => mockEngine.Object);
        services.AddScoped(_ => mockUnitOfWork.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var backgroundService = new WorkflowBackgroundService(scopeFactory, NullLogger<WorkflowBackgroundService>.Instance);
        
        using var cts = new CancellationTokenSource();
        
        // Act
        // Start the background service
        var execTask = backgroundService.StartAsync(cts.Token);
        
        // Let it poll at least once
        await Task.Delay(100);
        
        // Cancel to stop the loop
        cts.Cancel();
        
        try
        {
            await execTask;
            await backgroundService.StopAsync(CancellationToken.None);
        }
        catch (TaskCanceledException)
        {
            // Expected
        }

        // Assert
        mockInstanceRepo.Verify(x => x.GetRunningInstancesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        mockEngine.Verify(x => x.AdvanceAsync(instance, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        mockInstanceRepo.Verify(x => x.UpdateAsync(instance, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
