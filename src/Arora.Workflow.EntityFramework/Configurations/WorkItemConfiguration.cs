using Arora.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arora.Workflow.EntityFramework.Configurations;

internal sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("aw_workflow_work_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.WorkType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.LockedBy)
            .HasMaxLength(256);
            
        builder.HasIndex(x => new { x.Status, x.AvailableAt });
    }
}
