using Arora.Workflow.Domain.Aggregates;
using Arora.Workflow.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arora.Workflow.EntityFramework.Configurations;

/// <summary>
/// EF Core type configuration for <see cref="Approval"/>.
/// Maps to the <c>aw_approvals</c> table.
/// </summary>
internal sealed class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.ToTable("aw_approvals");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.WorkflowInstanceId).IsRequired();

        builder.Property(x => x.WorkflowName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.StepName)
            .IsRequired()
            .HasMaxLength(200);

        builder.OwnsOne(x => x.AssignedActor, actor =>
        {
            actor.Property(a => a.Id)
                .HasColumnName("assigned_actor_id")
                .IsRequired()
                .HasMaxLength(500);

            actor.Property(a => a.DisplayName)
                .HasColumnName("assigned_actor_name")
                .IsRequired()
                .HasMaxLength(500);
        });

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>();

        builder.Property(x => x.Comment)
            .HasMaxLength(4000);

        builder.OwnsOne(x => x.DecidedByActor, actor =>
        {
            actor.Property(a => a.Id)
                .HasColumnName("decided_by_actor_id")
                .HasMaxLength(500);

            actor.Property(a => a.DisplayName)
                .HasColumnName("decided_by_actor_name")
                .HasMaxLength(500);
        });

        // Performance: pending approvals for a tenant
        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("ix_aw_approvals_tenant_status");

        // Performance: all approvals for a workflow instance
        builder.HasIndex(x => new { x.TenantId, x.WorkflowInstanceId })
            .HasDatabaseName("ix_aw_approvals_tenant_instance");

        // Performance: escalation scheduler queries for approvals approaching deadline
        builder.HasIndex(x => new { x.Status, x.DeadlineAt })
            .HasDatabaseName("ix_aw_approvals_status_deadline");
    }
}

/// <summary>
/// EF Core type configuration for <see cref="WorkflowHistoryEntity"/>.
/// Maps to the <c>aw_workflow_history</c> table.
/// </summary>
internal sealed class WorkflowHistoryConfiguration
    : IEntityTypeConfiguration<WorkflowHistoryEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowHistoryEntity> builder)
    {
        builder.ToTable("aw_workflow_history");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.WorkflowInstanceId).IsRequired();

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.FromState).HasMaxLength(200);
        builder.Property(x => x.ToState).HasMaxLength(200);
        builder.Property(x => x.StepName).HasMaxLength(200);
        builder.Property(x => x.ActorId).HasMaxLength(500);
        builder.Property(x => x.ActorName).HasMaxLength(500);
        builder.Property(x => x.Comment).HasMaxLength(4000);

        // History is always queried by instance, ordered by time
        builder.HasIndex(x => new { x.TenantId, x.WorkflowInstanceId, x.OccurredAt })
            .HasDatabaseName("ix_aw_workflow_history_tenant_instance_time");
    }
}
