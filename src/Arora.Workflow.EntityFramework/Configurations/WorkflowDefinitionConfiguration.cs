using Arora.Workflow.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arora.Workflow.EntityFramework.Configurations;

/// <summary>
/// EF Core type configuration for the <see cref="WorkflowDefinition"/> aggregate.
/// Maps to the <c>aw_workflow_definitions</c> table.
/// </summary>
internal sealed class WorkflowDefinitionConfiguration
    : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("aw_workflow_definitions");

        // ── Primary key ──────────────────────────────────────────────────────
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever(); // IDs are assigned by the domain, not the database

        // ── Required columns ─────────────────────────────────────────────────
        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>(); // persists enum name, not integer

        builder.Property(x => x.DefinitionJson)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.ModifiedBy)
            .IsRequired()
            .HasMaxLength(500);

        // ── Optional columns ─────────────────────────────────────────────────
        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        // ── Indexes ──────────────────────────────────────────────────────────
        // Unique: one definition per (tenant, name, version)
        builder.HasIndex(x => new { x.TenantId, x.Name, x.Version })
            .IsUnique()
            .HasDatabaseName("ix_aw_workflow_definitions_tenant_name_version");

        // Query filter: isolates queries by tenant automatically (ADR-006)
        // Applied in AroraWorkflowModelBuilderExtensions where the TenantId is accessible
    }
}
