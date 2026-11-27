namespace SmartFactory.Platform.Api.Tests.Infrastructure;

/// <summary>Finds files in the repository regardless of where the test runner's working directory is.</summary>
internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    public static string PlantSpec => Path.Combine(Root, "docs", "plant", "plant.yaml");

    public static string OpenApiContract => Path.Combine(Root, "contracts", "openapi", "platform-api.v1.json");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SmartFactory.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root (SmartFactory.slnx) not found.");
    }
}
