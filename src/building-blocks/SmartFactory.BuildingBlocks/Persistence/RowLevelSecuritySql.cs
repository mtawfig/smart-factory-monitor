namespace SmartFactory.BuildingBlocks.Persistence;

/// <summary>
/// SQL used by module migrations to create the application role, grant it the minimum it needs, and enable
/// row-level security. Kept in one place so every module applies the same policy shape (ADR-008).
/// </summary>
public static class RowLevelSecuritySql
{
    public const string ApplicationRole = "sf_app";

    /// <summary>Creates the NOLOGIN application role once. Idempotent; safe in every module's first migration.</summary>
    public static string EnsureApplicationRole() => $"""
        DO $$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{ApplicationRole}') THEN
                CREATE ROLE {ApplicationRole} NOLOGIN;
            END IF;
        END
        $$;
        """;

    /// <summary>Lets the application role use a schema.</summary>
    public static string GrantSchemaUsage(string schema) => $"GRANT USAGE ON SCHEMA {schema} TO {ApplicationRole};";

    /// <summary>Grants table privileges, e.g. <c>"SELECT, INSERT, UPDATE, DELETE"</c> or <c>"SELECT, INSERT"</c>.</summary>
    public static string GrantTable(string schema, string table, string privileges) =>
        $"GRANT {privileges} ON {schema}.{table} TO {ApplicationRole};";

    /// <summary>
    /// Enables and forces RLS with a single policy: a row is visible and writable only when its tenant column equals
    /// the session's <c>app.tenant_id</c>. <c>current_setting(..., true)</c> returns NULL when unset, and
    /// <c>NULL = x</c> is never true, so an unset tenant sees nothing.
    /// </summary>
    public static string EnableTenantPolicy(string schema, string table, string tenantColumn = "tenant_id") => $"""
        ALTER TABLE {schema}.{table} ENABLE ROW LEVEL SECURITY;
        ALTER TABLE {schema}.{table} FORCE ROW LEVEL SECURITY;
        CREATE POLICY tenant_isolation ON {schema}.{table}
            USING ({tenantColumn} = NULLIF(current_setting('{TenantSessionInterceptor.TenantSetting}', true), ''))
            WITH CHECK ({tenantColumn} = NULLIF(current_setting('{TenantSessionInterceptor.TenantSetting}', true), ''));
        """;

    public static string DisableTenantPolicy(string schema, string table) => $"""
        DROP POLICY IF EXISTS tenant_isolation ON {schema}.{table};
        ALTER TABLE {schema}.{table} NO FORCE ROW LEVEL SECURITY;
        ALTER TABLE {schema}.{table} DISABLE ROW LEVEL SECURITY;
        """;
}
