namespace SmartFactory.BuildingBlocks.Persistence;

/// <summary>Binds the <c>Database</c> configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Name of the connection string shared by all modules (one database, one schema per module).</summary>
    public const string ConnectionStringName = "Platform";

    /// <summary>
    /// PostgreSQL role the application switches to on every connection (<c>SET ROLE</c>). Row-level security does
    /// not apply to superusers or table owners, so the application must run as this unprivileged role. Empty
    /// disables the switch (only acceptable when the login itself is already unprivileged, as in Azure).
    /// </summary>
    public string ApplicationRole { get; set; } = "sf_app";

    /// <summary>Apply EF Core migrations at start-up. Local and test environments only.</summary>
    public bool MigrateOnStartup { get; set; }
}

/// <summary>
/// Marks code that needs the login's own privileges (migrations only). Inside the scope the session interceptor
/// skips <c>SET ROLE</c>; everywhere else the application runs as <see cref="DatabaseOptions.ApplicationRole"/>.
/// </summary>
public static class ElevatedDatabaseScope
{
    private static readonly AsyncLocal<bool> Active = new();

    internal static bool IsActive => Active.Value;

    public static IDisposable Begin()
    {
        var previous = Active.Value;
        Active.Value = true;
        return new Restore(() => Active.Value = previous);
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
