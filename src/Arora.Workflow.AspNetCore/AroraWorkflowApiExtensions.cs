using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Arora.Workflow.Management;
using Arora.Workflow.Management.Models;

namespace Arora.Workflow.AspNetCore;

public static class AroraWorkflowApiExtensions
{
    /// <summary>
    /// Maps the Arora Workflow Management endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapAroraWorkflowApi(this IEndpointRouteBuilder endpoints, string prefix = "/arora/api/v1")
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("AroraWorkflow");

        group.MapGet("/definitions", async (
            IWorkflowQueryService queryService,
            [AsParameters] WorkflowDefinitionFilter filter) =>
        {
            var result = await queryService.GetDefinitionsAsync(filter);
            return Results.Ok(result);
        })
        .WithName("listWorkflowDefinitions")
        .Produces<PagedResult<WorkflowDefinitionSummary>>()
        .WithOpenApi();

        group.MapGet("/definitions/{id:guid}", async (
            Guid id,
            IWorkflowQueryService queryService) =>
        {
            var result = await queryService.GetDefinitionDetailsAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("getWorkflowDefinitionDetails")
        .Produces<WorkflowDefinitionDetails>()
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi();

        group.MapGet("/instances", async (
            IWorkflowQueryService queryService,
            [AsParameters] WorkflowInstanceFilter filter) =>
        {
            var result = await queryService.GetInstancesAsync(filter);
            return Results.Ok(result);
        })
        .WithName("listWorkflowInstances")
        .Produces<PagedResult<WorkflowInstanceSummary>>()
        .WithOpenApi();

        group.MapGet("/instances/{id:guid}", async (
            Guid id,
            IWorkflowQueryService queryService) =>
        {
            var result = await queryService.GetInstanceDetailsAsync(id);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("getWorkflowInstance")
        .Produces<WorkflowInstanceDetails>()
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi();

        group.MapGet("/instances/{id:guid}/history", async (
            Guid id,
            int page,
            int pageSize,
            IWorkflowQueryService queryService) =>
        {
            // Default parameters logic if not provided by query string can be handled by ASP.NET Core binding
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 50;

            var result = await queryService.GetInstanceHistoryAsync(id, page, pageSize);
            return Results.Ok(result);
        })
        .WithName("getWorkflowInstanceHistory")
        .Produces<PagedResult<WorkflowHistoryItem>>()
        .WithOpenApi();

        return endpoints;
    }
}
