using System.Reflection;
using NetArchTest.Rules;
using SmartFactory.BuildingBlocks.Tenancy;
using SmartFactory.Modules.Assets;
using SmartFactory.Modules.Audit;
using SmartFactory.Modules.Inventory;
using SmartFactory.Modules.Maintenance;

namespace SmartFactory.Platform.ArchitectureTests;

/// <summary>
/// Fitness functions for ADR-002 (modular monolith) and ADR-001 (BuildingBlocks is leaf-level shared code).
/// If one of these fails, the fix is to move the dependency behind a contract or an event, not to
/// change the test.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly Assembly Host = typeof(Program).Assembly;
    private static readonly Assembly BuildingBlocks = typeof(ICurrentTenant).Assembly;

    private static readonly IReadOnlyDictionary<string, Assembly> Modules = new Dictionary<string, Assembly>
    {
        ["SmartFactory.Modules.Assets"] = typeof(AssetsModule).Assembly,
        ["SmartFactory.Modules.Maintenance"] = typeof(MaintenanceModule).Assembly,
        ["SmartFactory.Modules.Inventory"] = typeof(InventoryModule).Assembly,
        ["SmartFactory.Modules.Audit"] = typeof(AuditModule).Assembly,
    };

    public static TheoryData<string> ModuleNames => [.. Modules.Keys];

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void A_module_does_not_depend_on_any_other_module(string moduleName)
    {
        var others = Modules.Keys.Where(k => k != moduleName).ToArray();

        var result = Types.InAssembly(Modules[moduleName])
            .ShouldNot().HaveDependencyOnAny(others)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{moduleName} must not reference {string.Join(", ", others)}; offending types: {Describe(result)}");
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void A_module_does_not_depend_on_the_host(string moduleName)
    {
        var result = Types.InAssembly(Modules[moduleName])
            .ShouldNot().HaveDependencyOn(Host.GetName().Name)
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"offending types: {Describe(result)}");
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void A_module_exposes_only_its_module_class(string moduleName)
    {
        // EF Core generates migrations as public classes; they are data, not an API, and are excluded.
        var publicTypes = Modules[moduleName].GetExportedTypes()
            .Where(t => !t.IsNested)
            .Where(t => t.Namespace?.EndsWith(".Persistence.Migrations", StringComparison.Ordinal) != true)
            .Select(t => t.Name)
            .ToArray();

        publicTypes.Should().AllSatisfy(name => name.Should().EndWith("Module"),
            "a module's public surface is its registration class; everything else is internal (ADR-002)");
    }

    [Fact]
    public void BuildingBlocks_does_not_depend_on_modules_or_host()
    {
        var forbidden = Modules.Keys.Append(Host.GetName().Name!).ToArray();

        var result = Types.InAssembly(BuildingBlocks)
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue($"offending types: {Describe(result)}");
    }

    [Fact]
    public void Nothing_in_the_platform_references_an_mqtt_client()
    {
        // ADR-004: the only MQTT subscriber is the Ingest Worker and the only publisher is the simulator.
        var assemblies = Modules.Values.Append(Host).Append(BuildingBlocks);

        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOn("MQTTnet").GetResult();
            result.IsSuccessful.Should().BeTrue($"{assembly.GetName().Name} references MQTTnet: {Describe(result)}");
        }
    }

    /// <summary>
    /// Every DbContext in every module, discovered rather than listed, so a new module cannot be forgotten. The schema
    /// comes from the module's public <c>Schema</c> constant.
    /// </summary>
    public static TheoryData<string, Type, string> ModuleContexts()
    {
        var data = new TheoryData<string, Type, string>();
        foreach (var (schema, context, moduleName) in DiscoverContexts())
        {
            data.Add(schema, context, moduleName);
        }

        return data;
    }

    [Fact]
    public void Modules_with_persistence_are_discovered()
    {
        DiscoverContexts().Select(c => c.ModuleName).Should().Contain(
            ["SmartFactory.Modules.Assets", "SmartFactory.Modules.Audit", "SmartFactory.Modules.Maintenance"]);
    }

    private static IEnumerable<(string Schema, Type Context, string ModuleName)> DiscoverContexts()
    {
        foreach (var (moduleName, assembly) in Modules)
        {
            var schema = (string)assembly.GetExportedTypes().Single(t => t.Name.EndsWith("Module", StringComparison.Ordinal)).GetField("Schema")!.GetRawConstantValue()!;
            foreach (var context in assembly.GetTypes().Where(t => !t.IsAbstract && typeof(Microsoft.EntityFrameworkCore.DbContext).IsAssignableFrom(t)))
            {
                yield return (schema, context, moduleName);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ModuleContexts))]
    public void A_module_maps_only_into_its_own_schema(string schema, Type contextType, string moduleName)
    {
        // ADR-007: one schema per module. Builds the EF model offline (no database) and checks every table.
        using var context = CreateOffline(contextType);

        var schemas = context.Model.GetEntityTypes().Select(e => Microsoft.EntityFrameworkCore.RelationalEntityTypeExtensions.GetSchema(e)).Distinct().ToList();

        schemas.Should().Equal([schema], $"{moduleName} must not map tables outside the '{schema}' schema");
        contextType.Assembly.GetName().Name.Should().Be(moduleName);
    }

    [Theory]
    [MemberData(nameof(ModuleContexts))]
    public void Every_tenant_scoped_entity_is_filtered_by_tenant(string schema, Type contextType, string moduleName)
    {
        // ADR-008 layer 2: an ITenantScoped entity without a global query filter would leak across tenants.
        using var context = CreateOffline(contextType);

        var unfiltered = context.Model.GetEntityTypes()
            .Where(e => typeof(ITenantScoped).IsAssignableFrom(e.ClrType))
            .Where(e => e.GetDeclaredQueryFilters().Count == 0)
            .Select(e => e.ClrType.Name)
            .ToList();

        unfiltered.Should().BeEmpty($"{moduleName} ({schema}): tenant-scoped entities need a query filter");
    }

    private static Microsoft.EntityFrameworkCore.DbContext CreateOffline(Type contextType)
    {
        var factoryType = contextType.Assembly.GetTypes().Single(t => t.Name == contextType.Name + "DesignTimeFactory");
        return (Microsoft.EntityFrameworkCore.DbContext)factoryType.GetMethod("CreateDbContext")!
            .Invoke(Activator.CreateInstance(factoryType), [Array.Empty<string>()])!;
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        result.FailingTypeNames is { Count: > 0 } names ? string.Join(", ", names) : "(none)";
}
