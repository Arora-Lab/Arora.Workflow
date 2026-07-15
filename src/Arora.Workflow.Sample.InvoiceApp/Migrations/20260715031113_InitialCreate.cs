using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arora.Workflow.Sample.InvoiceApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aw_approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    StepName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    assigned_actor_id = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    assigned_actor_name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeadlineAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    decided_by_actor_id = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    decided_by_actor_name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aw_approvals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aw_workflow_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DefinitionJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aw_workflow_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aw_workflow_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FromState = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ToState = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    StepName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ActorId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ActorName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    NodeId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aw_workflow_history", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aw_workflow_instances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowDefinitionVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkflowName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CurrentState = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    InputJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    created_by_id = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    created_by_name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    HistorySequence = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    row_version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aw_workflow_instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "aw_workflow_work_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LockedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Payload = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aw_workflow_work_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_aw_approvals_status_deadline",
                table: "aw_approvals",
                columns: new[] { "Status", "DeadlineAt" });

            migrationBuilder.CreateIndex(
                name: "ix_aw_approvals_tenant_instance",
                table: "aw_approvals",
                columns: new[] { "TenantId", "WorkflowInstanceId" });

            migrationBuilder.CreateIndex(
                name: "ix_aw_approvals_tenant_status",
                table: "aw_approvals",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_aw_workflow_definitions_tenant_name_version",
                table: "aw_workflow_definitions",
                columns: new[] { "TenantId", "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_aw_workflow_history_tenant_instance_time",
                table: "aw_workflow_history",
                columns: new[] { "TenantId", "WorkflowInstanceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "uq_aw_workflow_history_tenant_instance_sequence",
                table: "aw_workflow_history",
                columns: new[] { "TenantId", "WorkflowInstanceId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_aw_workflow_instances_tenant_correlation",
                table: "aw_workflow_instances",
                columns: new[] { "TenantId", "CorrelationId", "WorkflowDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "ix_aw_workflow_instances_tenant_idempotency",
                table: "aw_workflow_instances",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_aw_workflow_instances_tenant_status",
                table: "aw_workflow_instances",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_aw_workflow_work_items_Status_AvailableAt",
                table: "aw_workflow_work_items",
                columns: new[] { "Status", "AvailableAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aw_approvals");

            migrationBuilder.DropTable(
                name: "aw_workflow_definitions");

            migrationBuilder.DropTable(
                name: "aw_workflow_history");

            migrationBuilder.DropTable(
                name: "aw_workflow_instances");

            migrationBuilder.DropTable(
                name: "aw_workflow_work_items");
        }
    }
}
