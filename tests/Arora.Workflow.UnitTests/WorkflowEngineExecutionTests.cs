using System;
using System.Threading;
using System.Threading.Tasks;
using Arora.Workflow.Application.Steps;
using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.Internal.Engine;
using Arora.Workflow.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;

namespace Arora.Workflow.UnitTests.Engine;

public class MockExecutionStep : IWorkflowStep
{
    public static int ExecutionCount = 0;
    public static StepExecutionContext? LastContext;

    public Task<string?> ExecuteAsync(StepExecutionContext context)
    {
        ExecutionCount++;
        LastContext = context;
        if (context.Instance.CorrelationId == "UseMiddlewareResult")
            return Task.FromResult<string?>("OriginalResult");

        return Task.FromResult<string?>(null);
    }
}

public class TestMiddleware : Arora.Workflow.Application.Middleware.IWorkflowMiddleware
{
    public static List<string> InvocationLog = new();
    private readonly string _name;

    public TestMiddleware(string name)
    {
        _name = name;
    }

    public async Task<string?> InvokeAsync(StepExecutionContext context, Arora.Workflow.Application.Middleware.WorkflowStepDelegate next)
    {
        InvocationLog.Add($"{_name} - Before");
        var result = await next(context);
        InvocationLog.Add($"{_name} - After");
        return result;
    }
}

public class ConditionalStep : IWorkflowStep
{
    public Task<string?> ExecuteAsync(StepExecutionContext context)
    {
        return Task.FromResult<string?>("Success");
    }
}

public class WorkflowEngineExecutionTests
{
    public WorkflowEngineExecutionTests()
    {
        MockExecutionStep.ExecutionCount = 0;
        MockExecutionStep.LastContext = null;
        TestMiddleware.InvocationLog.Clear();
    }

    [Fact]
    public async Task AdvanceAsync_WithValidStepType_ExecutesStepLogicAndAdvances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<MockExecutionStep>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TestWorkflowHost).Assembly));
        var sp = services.BuildServiceProvider();

        var host = new TestWorkflowHost();
        var engine = new WorkflowEngine(host.DefinitionRepository, host.ApprovalRepository, host.Clock, sp);

        var json = $$"""
        {
            "InitialNode": "Step1",
            "Nodes": {
                "Step1": {
                    "Type": "Step",
                    "StepType": "{{typeof(MockExecutionStep).AssemblyQualifiedName}}",
                    "Transitions": []
                }
            }
        }
        """;

        var def = WorkflowDefinition.Create(Guid.NewGuid(), "test-def", 1, null, json, "sys", host.Clock.UtcNow);
        await host.DefinitionRepository.AddAsync(def);

        var initialState = new WorkflowState("Step1", WorkflowStateType.Initial);
        var instance = WorkflowInstance.Start(Guid.NewGuid(), def.Id, 1, "test-def", "corr1", "idemp1", initialState, null, new ActorInfo("sys", "sys"), host.Clock.UtcNow);

        // Act
        await engine.AdvanceAsync(instance, CancellationToken.None);

        // Assert
        Assert.Equal(1, MockExecutionStep.ExecutionCount);
        Assert.NotNull(MockExecutionStep.LastContext);
        Assert.Equal("Step1", MockExecutionStep.LastContext.StepName);
        Assert.Equal("Completed", instance.CurrentState);
    }

    [Fact]
    public async Task AdvanceAsync_WithConditionalStep_UsesConditionForTransition()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ConditionalStep>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TestWorkflowHost).Assembly));
        var sp = services.BuildServiceProvider();

        var host = new TestWorkflowHost();
        var engine = new WorkflowEngine(host.DefinitionRepository, host.ApprovalRepository, host.Clock, sp);

        var json = $$"""
        {
            "InitialNode": "Step1",
            "Nodes": {
                "Step1": {
                    "Type": "Step",
                    "StepType": "{{typeof(ConditionalStep).AssemblyQualifiedName}}",
                    "Transitions": [
                        { "Condition": "Failure", "TargetNode": "EndFailure" },
                        { "Condition": "Success", "TargetNode": "EndSuccess" }
                    ]
                }
            }
        }
        """;

        var def = WorkflowDefinition.Create(Guid.NewGuid(), "test-def", 1, null, json, "sys", host.Clock.UtcNow);
        await host.DefinitionRepository.AddAsync(def);

        var initialState = new WorkflowState("Step1", WorkflowStateType.Initial);
        var instance = WorkflowInstance.Start(Guid.NewGuid(), def.Id, 1, "test-def", "corr1", "idemp1", initialState, null, new ActorInfo("sys", "sys"), host.Clock.UtcNow);

        // Act
        await engine.AdvanceAsync(instance, CancellationToken.None);

        var parsedGraph = Arora.Workflow.Internal.Engine.Graph.WorkflowGraph.Parse(def.DefinitionJson);
        Assert.Equal("Success", parsedGraph.Nodes["Step1"].Transitions[1].Condition);
        Assert.Equal("EndSuccess", parsedGraph.Nodes["Step1"].Transitions[1].TargetNode);

        // Assert
        Assert.Equal("Completed", instance.CurrentState);
        Assert.Contains(instance.DomainEvents.OfType<Arora.Workflow.Domain.Events.WorkflowTransitioned>(), e => e.ToState == "EndSuccess");
    }

    [Fact]
    public async Task AdvanceAsync_WithMiddleware_ExecutesPipelineInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<MockExecutionStep>();
        services.AddScoped<Arora.Workflow.Application.Middleware.IWorkflowMiddleware>(sp => new TestMiddleware("Outer"));
        services.AddScoped<Arora.Workflow.Application.Middleware.IWorkflowMiddleware>(sp => new TestMiddleware("Inner"));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TestWorkflowHost).Assembly));
        var sp = services.BuildServiceProvider();

        var host = new TestWorkflowHost();
        var engine = new WorkflowEngine(host.DefinitionRepository, host.ApprovalRepository, host.Clock, sp);

        var json = $$"""
        {
            "InitialNode": "Step1",
            "Nodes": {
                "Step1": {
                    "Type": "Step",
                    "StepType": "{{typeof(MockExecutionStep).AssemblyQualifiedName}}",
                    "Transitions": []
                }
            }
        }
        """;

        var def = WorkflowDefinition.Create(Guid.NewGuid(), "test-def", 1, null, json, "sys", host.Clock.UtcNow);
        await host.DefinitionRepository.AddAsync(def);

        var initialState = new WorkflowState("Step1", WorkflowStateType.Initial);
        var instance = WorkflowInstance.Start(Guid.NewGuid(), def.Id, 1, "test-def", "UseMiddlewareResult", "idemp1", initialState, null, new ActorInfo("sys", "sys"), host.Clock.UtcNow);

        // Act
        await engine.AdvanceAsync(instance, CancellationToken.None);

        // Assert
        Assert.Equal(4, TestMiddleware.InvocationLog.Count);
        Assert.Equal("Outer - Before", TestMiddleware.InvocationLog[0]);
        Assert.Equal("Inner - Before", TestMiddleware.InvocationLog[1]);
        Assert.Equal("Inner - After", TestMiddleware.InvocationLog[2]);
        Assert.Equal("Outer - After", TestMiddleware.InvocationLog[3]);
        
        Assert.Equal(1, MockExecutionStep.ExecutionCount);
    }
}
