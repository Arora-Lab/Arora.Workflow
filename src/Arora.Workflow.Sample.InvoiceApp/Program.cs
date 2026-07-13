using Arora.Workflow;
using Arora.Workflow.EntityFramework.Context;
using Arora.Workflow.Sample.InvoiceApp.Workflows;
using Microsoft.EntityFrameworkCore;
using Arora.Workflow.Application.Interfaces;
using Arora.Workflow.Domain.ValueObjects;
using Arora.Workflow.Domain.Aggregates;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SQLite Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=arora.db");
});

// Register Arora Workflow
builder.Services.AddAroraWorkflow()
    .UseEntityFramework<AppDbContext>();

// Register a fake TenantContext for the sample
builder.Services.AddScoped<ITenantContext, FakeTenantContext>();

// Register Steps
builder.Services.AddTransient<Arora.Workflow.Sample.InvoiceApp.Workflows.Steps.ValidateInvoiceStep>();
builder.Services.AddTransient<Arora.Workflow.Sample.InvoiceApp.Workflows.Steps.ProcessPaymentStep>();
builder.Services.AddTransient<Arora.Workflow.Sample.InvoiceApp.Workflows.Steps.SendRejectionStep>();

var app = builder.Build();

// Create the database automatically and seed definition
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    
    var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
    var definitionRepo = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
    var existing = definitionRepo.GetLatestPublishedAsync("invoice-approval").GetAwaiter().GetResult();
    if (existing == null)
    {
        var provider = new InvoiceApprovalWorkflow();
        var def = Arora.Workflow.Domain.Aggregates.WorkflowDefinition.Create(
            tenantContext.TenantId,
            "invoice-approval",
            1,
            "Basic Invoice Approval Process",
            provider.GetDefinitionJson(),
            "system",
            DateTimeOffset.UtcNow);
        
        def.Publish("system", DateTimeOffset.UtcNow);
        definitionRepo.AddAsync(def).GetAwaiter().GetResult();
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ---- Minimal APIs ----

var api = app.MapGroup("/api/invoices");

api.MapPost("/", async (IWorkflowService workflowService, InvoiceRequest request, CancellationToken ct) =>
{
    var input = new JsonObject
    {
        ["InvoiceId"] = request.InvoiceId,
        ["Amount"] = request.Amount,
        ["VendorId"] = request.VendorId
    };

    var result = await workflowService.StartAsync(new StartWorkflowRequest
    {
        WorkflowName = "invoice-approval",
        CorrelationId = request.InvoiceId,
        IdempotencyKey = $"invoice-{request.InvoiceId}",
        Input = input,
        InitiatedBy = new ActorInfo("user1", "John Doe")
    }, ct);

    return Results.Ok(new { WorkflowInstanceId = result });
});

api.MapGet("/{id}/status", async (Guid id, IWorkflowInstanceRepository repo, CancellationToken ct) =>
{
    var instance = await repo.GetByIdAsync(id, ct);
    if (instance == null) return Results.NotFound();

    return Results.Ok(new
    {
        instance.Id,
        instance.Status,
        CurrentState = instance.CurrentState
    });
});

api.MapGet("/{id}/history", async (Guid id, IWorkflowHistoryRepository repo, CancellationToken ct) =>
{
    var history = await repo.GetByInstanceIdAsync(id, ct);
    return Results.Ok(history);
});

var approvalApi = app.MapGroup("/api/approvals");

approvalApi.MapGet("/pending/{user}", async (string user, IApprovalService approvalService, CancellationToken ct) =>
{
    var approvals = await approvalService.GetPendingApprovalsAsync(new ActorInfo(user, user), ct);
    return Results.Ok(approvals);
});

approvalApi.MapPost("/{id}/approve", async (Guid id, IApprovalService approvalService, CancellationToken ct) =>
{
    await approvalService.ApproveAsync(id, new ActorInfo("tester", "Tester"), "Looks good", ct);
    return Results.Ok();
});

approvalApi.MapPost("/{id}/reject", async (Guid id, IApprovalService approvalService, CancellationToken ct) =>
{
    await approvalService.RejectAsync(id, new ActorInfo("tester", "Tester"), "Amount too high", ct);
    return Results.Ok();
});

app.Run();

public record InvoiceRequest(string InvoiceId, decimal Amount, string VendorId);

public class FakeTenantContext : ITenantContext
{
    public Guid TenantId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyAroraWorkflowMappings();
        base.OnModelCreating(builder);
    }
}
