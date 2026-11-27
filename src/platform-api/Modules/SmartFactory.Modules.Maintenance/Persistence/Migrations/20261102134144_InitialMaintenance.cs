using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartFactory.BuildingBlocks.Persistence;

#nullable disable

namespace SmartFactory.Modules.Maintenance.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMaintenance : Migration
    {
        private static readonly string[] Tables = ["requests", "work_orders", "checklist_items", "work_order_history", "findings", "attachments"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "maintenance");

            migrationBuilder.CreateTable(
                name: "requests",
                schema: "maintenance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    requested_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decision = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_orders",
                schema: "maintenance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    held_from = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    assigned_to = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    scheduled_for = table.Column<DateOnly>(type: "date", nullable: true),
                    approved_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    disposition = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    status_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_orders_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "maintenance",
                        principalTable: "requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checklist_items",
                schema: "maintenance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_checklist_items_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "maintenance",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "findings",
                schema: "maintenance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    technician = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    condition = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_findings", x => x.id);
                    table.ForeignKey(
                        name: "fk_findings_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "maintenance",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_history",
                schema: "maintenance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    from_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    to_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    actor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_order_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_order_history_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalSchema: "maintenance",
                        principalTable: "work_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                schema: "maintenance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    finding_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    content_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    uploaded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_attachments_findings_finding_id",
                        column: x => x.finding_id,
                        principalSchema: "maintenance",
                        principalTable: "findings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attachments_finding_id",
                schema: "maintenance",
                table: "attachments",
                column: "finding_id");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_items_work_order_id",
                schema: "maintenance",
                table: "checklist_items",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_findings_work_order_id",
                schema: "maintenance",
                table: "findings",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_requests_tenant_id_asset_id",
                schema: "maintenance",
                table: "requests",
                columns: new[] { "tenant_id", "asset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_work_order_history_work_order_id_revision",
                schema: "maintenance",
                table: "work_order_history",
                columns: new[] { "work_order_id", "revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_request_id",
                schema: "maintenance",
                table: "work_orders",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_tenant_id_asset_id",
                schema: "maintenance",
                table: "work_orders",
                columns: new[] { "tenant_id", "asset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_tenant_id_assigned_to",
                schema: "maintenance",
                table: "work_orders",
                columns: new[] { "tenant_id", "assigned_to" });

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_tenant_id_status",
                schema: "maintenance",
                table: "work_orders",
                columns: new[] { "tenant_id", "status" });

            // ── Hand-written: least-privilege grants and row-level security (ADR-008) ──
            migrationBuilder.Sql(RowLevelSecuritySql.EnsureApplicationRole());
            migrationBuilder.Sql(RowLevelSecuritySql.GrantSchemaUsage("maintenance"));
            // Work orders and requests are never deleted by the application; cancelled is a state, not a deletion.
            migrationBuilder.Sql(RowLevelSecuritySql.GrantTable("maintenance", "requests", "SELECT, INSERT, UPDATE"));
            migrationBuilder.Sql(RowLevelSecuritySql.GrantTable("maintenance", "work_orders", "SELECT, INSERT, UPDATE"));
            // Editing the plan replaces checklist lines, so they can be deleted.
            migrationBuilder.Sql(RowLevelSecuritySql.GrantTable("maintenance", "checklist_items", "SELECT, INSERT, UPDATE, DELETE"));
            // History, findings and attachments are evidence: insert and read only.
            foreach (var table in new[] { "work_order_history", "findings", "attachments" })
            {
                migrationBuilder.Sql(RowLevelSecuritySql.GrantTable("maintenance", table, "SELECT, INSERT"));
            }

            foreach (var table in Tables)
            {
                migrationBuilder.Sql(RowLevelSecuritySql.EnableTenantPolicy("maintenance", table));
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.Sql(RowLevelSecuritySql.DisableTenantPolicy("maintenance", table));
            }

            migrationBuilder.DropTable(
                name: "attachments",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "checklist_items",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "work_order_history",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "findings",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "work_orders",
                schema: "maintenance");

            migrationBuilder.DropTable(
                name: "requests",
                schema: "maintenance");
        }
    }
}
