using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Arora.Workflow;

/// <summary>
/// Extension methods on <see cref="AroraWorkflowBuilder"/> for wiring
/// EF Core persistence into Arora.Workflow.
/// </summary>
public static class AroraWorkflowEntityFrameworkExtensions
{
    public static AroraWorkflowBuilder UseEntityFramework<TDbContext>(
        this AroraWorkflowBuilder builder)
        where TDbContext : DbContext
    {
        builder.Services.AddScoped(sp => new Arora.Workflow.EntityFramework.DbContextProvider(sp.GetRequiredService<TDbContext>()));


        builder.Services.AddScoped<IWorkflowDefinitionRepository, EfCoreWorkflowDefinitionRepository>();
        builder.Services.AddScoped<IWorkflowInstanceRepository, EfCoreWorkflowInstanceRepository>();
        builder.Services.AddScoped<IApprovalRepository, EfCoreApprovalRepository>();
        builder.Services.AddScoped<IWorkflowHistoryRepository, EfCoreWorkflowHistoryRepository>();
        builder.Services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();

        return builder;
    }
}
