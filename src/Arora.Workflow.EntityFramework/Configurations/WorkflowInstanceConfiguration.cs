using Arora.Workflow.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arora.Workflow.EntityFramework.Configurations;

/// <summary>
/// EF Core type configuration for the <see cref="WorkflowInstance"/> aggregate.
/// Maps to the <c>aw_workflow_instances</c> table.
/// </summary>
internal sealed class WorkflowInstanceConfiguration
    : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("aw_workflow_instances");

        // ── Primary key ──────────────────────────────────────────────────────
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // ── Scalar columns ───────────────────────────────────────────────────
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.WorkflowDefinitionId).IsRequired();
        builder.Property(x => x.WorkflowDefinitionVersion).IsRequired();

        builder.Property(x => x.WorkflowName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.CurrentState)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>(); // persists "Running", "PendingApproval", etc.

        builder.Property(x => x.InputJson)
            .HasColumnType("text");

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedAt);

        builder.Property(x => x.HistorySequence)
            .IsRequired()
            .HasDefaultValue(0L);

        // ── ActorInfo owned value object ─────────────────────────────────────
        // CreatedBy is an ActorInfo record (Id + DisplayName).
        // Stored as two columns in the same table — no join required.
        builder.OwnsOne(x => x.CreatedBy, actor =>
        {
            actor.Property(a => a.Id)
                .HasColumnName("created_by_id")
                .IsRequired()
                .HasMaxLength(500);

            actor.Property(a => a.DisplayName)
                .HasColumnName("created_by_name")
                .IsRequired()
                .HasMaxLength(500);
        });

        // ── Optimistic concurrency ───────────────────────────────────────────
        // Prevents two concurrent processes from overwriting each other's state.
        // EF Core will check this token on every UPDATE.
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion()
            .HasColumnName("row_version");

        // ── Indexes ──────────────────────────────────────────────────────────
        // Unique: one instance per idempotency key per tenant
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ix_aw_workflow_instances_tenant_idempotency");

        // Performance: common query — find instance by business entity
        builder.HasIndex(x => new { x.TenantId, x.CorrelationId, x.WorkflowDefinitionId })
            .HasDatabaseName("ix_aw_workflow_instances_tenant_correlation");

        // Performance: dashboard queries by status
        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("ix_aw_workflow_instances_tenant_status");

        // ── Ignore domain event collection ───────────────────────────────────
        // DomainEvents is an in-memory list. EF Core must not try to persist it.
        builder.Ignore(x => x.DomainEvents);
    }
}
