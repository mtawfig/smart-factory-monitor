using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartFactory.BuildingBlocks.Persistence;

#nullable disable

namespace SmartFactory.Modules.Assets.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "assets");

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "assets",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plants",
                schema: "assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plants", x => x.id);
                    table.ForeignKey(
                        name: "fk_plants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "assets",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lines",
                schema: "assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    plant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_lines_plants_plant_id",
                        column: x => x.plant_id,
                        principalSchema: "assets",
                        principalTable: "plants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assets",
                schema: "assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    asset_class = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    criticality = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rated_power_kw = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_assets_lines_line_id",
                        column: x => x.line_id,
                        principalSchema: "assets",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sensor_mappings",
                schema: "assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    interval_seconds = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    nominal_min = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    nominal_max = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    kind = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: true),
                    source_topic = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    active_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensor_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_sensor_mappings_assets_asset_id",
                        column: x => x.asset_id,
                        principalSchema: "assets",
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assets_line_id",
                schema: "assets",
                table: "assets",
                column: "line_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_code",
                schema: "assets",
                table: "assets",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lines_plant_id",
                schema: "assets",
                table: "lines",
                column: "plant_id");

            migrationBuilder.CreateIndex(
                name: "ix_lines_tenant_id_plant_id_code",
                schema: "assets",
                table: "lines",
                columns: new[] { "tenant_id", "plant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plants_tenant_id_code",
                schema: "assets",
                table: "plants",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sensor_mappings_asset_id",
                schema: "assets",
                table: "sensor_mappings",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensor_mappings_source_topic",
                schema: "assets",
                table: "sensor_mappings",
                column: "source_topic");

            migrationBuilder.CreateIndex(
                name: "ix_sensor_mappings_tenant_id_asset_id_metric",
                schema: "assets",
                table: "sensor_mappings",
                columns: new[] { "tenant_id", "asset_id", "metric" },
                unique: true);

            // ── Hand-written: least privilege and row-level security (ADR-008) ──────────────────────────────
            // The application runs as sf_app (SET ROLE on every connection), never as the table owner, because
            // PostgreSQL does not apply RLS to owners or superusers.
            migrationBuilder.Sql(RowLevelSecuritySql.EnsureApplicationRole());
            migrationBuilder.Sql(RowLevelSecuritySql.GrantSchemaUsage("assets"));
            // Tenants are created by seeding/administration only; the application never deletes them.
            migrationBuilder.Sql(RowLevelSecuritySql.GrantTable("assets", "tenants", "SELECT, INSERT"));
            foreach (var table in new[] { "plants", "lines", "assets", "sensor_mappings" })
            {
                migrationBuilder.Sql(RowLevelSecuritySql.GrantTable("assets", table, "SELECT, INSERT, UPDATE, DELETE"));
                migrationBuilder.Sql(RowLevelSecuritySql.EnableTenantPolicy("assets", table));
            }

            migrationBuilder.Sql(RowLevelSecuritySql.EnableTenantPolicy("assets", "tenants", tenantColumn: "id"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "tenants", "plants", "lines", "assets", "sensor_mappings" })
            {
                migrationBuilder.Sql(RowLevelSecuritySql.DisableTenantPolicy("assets", table));
            }

            migrationBuilder.DropTable(
                name: "sensor_mappings",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "assets",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "lines",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "plants",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "assets");
        }
    }
}
