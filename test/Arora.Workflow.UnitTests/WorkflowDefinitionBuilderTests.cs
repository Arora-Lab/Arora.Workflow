using System.Text.Json;
using Arora.Workflow.Application.Builder;
using Arora.Workflow.Internal.Engine.Graph;

namespace Arora.Workflow.UnitTests.Application.Builder;

public class DummyStep { }

public class WorkflowDefinitionBuilderTests
{
    [Fact]
    public void BuildJson_WithStepAndApproval_GeneratesExpectedGraph()
    {
        // Arrange
        var builder = WorkflowDefinitionBuilder.Create("TestWorkflow");
        builder
            .WithStep<DummyStep>("Step1")
            .TransitionsTo("Approval1")
            .WithApproval("Approval1")
            .OnApprove("Step2")
            .OnReject("End")
            .EndApproval()
            .WithStep<DummyStep>("Step2");

        // Act
        var definition = builder.Build();
        var json = definition.Json;

        // Assert
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var graph = JsonSerializer.Deserialize<WorkflowGraph>(json, options);

        Assert.NotNull(graph);
        Assert.Equal("Step1", graph.InitialNode);
        Assert.Equal(3, graph.Nodes.Count);

        // Verify Step1
        var step1 = graph.Nodes["Step1"];
        Assert.Equal("Step", step1.Type);
        Assert.Equal(typeof(DummyStep).AssemblyQualifiedName, step1.StepType);
        Assert.Single(step1.Transitions);
        Assert.Equal("Approval1", step1.Transitions[0].TargetNode);
        Assert.Null(step1.Transitions[0].Condition);

        // Verify Approval1
        var approval1 = graph.Nodes["Approval1"];
        Assert.Equal("Approval", approval1.Type);
        Assert.Null(approval1.StepType);
        Assert.Equal(2, approval1.Transitions.Count);
        
        var approveTransition = approval1.Transitions.Single(t => t.Condition == "Approved");
        Assert.Equal("Step2", approveTransition.TargetNode);
        
        var rejectTransition = approval1.Transitions.Single(t => t.Condition == "Rejected");
        Assert.Equal("End", rejectTransition.TargetNode);

        // Verify Step2
        var step2 = graph.Nodes["Step2"];
        Assert.Empty(step2.Transitions);
    }
}
