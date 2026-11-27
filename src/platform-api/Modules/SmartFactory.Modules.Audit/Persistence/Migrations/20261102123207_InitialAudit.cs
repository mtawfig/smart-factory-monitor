using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartFactory.BuildingBlocks.Persistence;

#nullable disable

namespace SmartFactory.Modules.Audit.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    actor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    aggregate_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    before_state = table.Column<string>(type: "text", nullable: true),
                    after_state = table.Column<string>(type: "text", nullable: true),
                    before_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    after_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    previous_chain_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    chain_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_tenant_id_aggregate_type_aggregate_id",
                schema: "audit",
                table: "audit_events",
                columns: new[] { "tenant_id", "aggregate_type", "aggregate_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_tenant_id_sequence",
                schema: "audit",
                table: "audit_events",
                columns: new[] { "tenant_id", "sequence" },
                unique: true);

            // ── Hand-written: append-only enforcement, least privilege, row-level security ─────────────────────
            // The trigger rejects UPDATE, DELETE and TRUNCATE for every role, including the owner, so "append-only"
            // does not depend on someone remembering to revoke a grant.
            migrationBuilder.Sql("""
                CREATE FUNCTION audit.reject_mutation() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'audit.audit_events is append-only: % is not allowed', TG_OP
                        USING ERRCODE = 'insufficient_privilege';
                END
                $$;
                CREATE TRIGGER audit_events_append_only
                    BEFORE UPDATE OR DELETE ON audit.audit_events
                    FOR EACH ROW EXECUTE FUNCTION audit.reject_mutation();
                CREATE TRIGGER audit_events_no_truncate
                    BEFORE TRUNCATE ON audit.audit_events
                    FOR EACH STATEMENT EXECUTE FUNCTION audit.reject_mutation();
                """);
            migrationBuilder.Sql(RowLevelSecuritySql.EnsureApplicationRole());
            migrationBuilder.Sql(RowLevelSecuritySql.GrantSchemaUsage("audit"));
            migrationBuilder.Sql(RowLevelSecuritySql.GrantTable("audit", "audit_events", "SELECT, INSERT"));
            migrationBuilder.Sql(RowLevelSecuritySql.EnableTenantPolicy("audit", "audit_events"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RowLevelSecuritySql.DisableTenantPolicy("audit", "audit_events"));
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS audit_events_no_truncate ON audit.audit_events;
                DROP TRIGGER IF EXISTS audit_events_append_only ON audit.audit_events;
                DROP FUNCTION IF EXISTS audit.reject_mutation();
                """);

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "audit");
        }
    }
}
