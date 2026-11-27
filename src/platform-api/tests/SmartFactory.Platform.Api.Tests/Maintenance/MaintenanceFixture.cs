using System.Net;
using System.Net.Http.Json;
using SmartFactory.BuildingBlocks.Auth;
using SmartFactory.Platform.Api.Tests.Infrastructure;

namespace SmartFactory.Platform.Api.Tests.Maintenance;

/// <summary>People and helpers shared by the maintenance tests. Usernames matter: assignment is by username.</summary>
internal static class Crew
{
    public const string Tech1 = "tech.one";
    public const string Tech2 = "tech.two";

    public static HttpClient Planner(this PlatformApiFactory f, string tenant = TestUsers.TenantA) =>
        f.CreateClient().AsUser(tenant, "planner", Roles.MaintenancePlanner);

    public static HttpClient Manager(this PlatformApiFactory f, string tenant = TestUsers.TenantA) =>
        f.CreateClient().AsUser(tenant, "manager", Roles.ReliabilityManager);

    public static HttpClient Technician(this PlatformApiFactory f, string username = Tech1, string tenant = TestUsers.TenantA) =>
        f.CreateClient().AsUser(tenant, username, Roles.Technician);

    public static async Task<Guid> AssetIdAsync(this PlatformApiFactory f, string code, string tenant, CancellationToken ct)
    {
        var assets = await f.CreateClient().As(tenant, Roles.MaintenancePlanner)
            .GetFromJsonAsync<List<AssetSummaryModel>>($"/api/assets/assets?search={code}", ct);
        return assets!.Single(a => a.Code == code).Id;
    }

    public static async Task<WorkOrderDetailModel> RaiseAsync(this PlatformApiFactory f, CancellationToken ct, string tenant = TestUsers.TenantA, params string[] checklist)
    {
        var assetId = await f.AssetIdAsync("MTR-042", tenant, ct);
        var response = await f.Planner(tenant).PostAsJsonAsync("/api/maintenance/requests", new
        {
            assetId,
            title = "Bearing noise on filler drive motor",
            reason = "Vibration above the alarm band for three windows (synthetic data).",
            priority = "High",
            checklist = checklist.Length > 0 ? checklist : ["Check bearing play", "Measure vibration"],
            dueDate = "2026-11-10",
        }, ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(ct));
        return (await response.Content.ReadFromJsonAsync<WorkOrderDetailModel>(ct))!;
    }

    /// <summary>Raised, approved by the manager and scheduled for <paramref name="technician"/>.</summary>
    public static async Task<WorkOrderDetailModel> ScheduledAsync(this PlatformApiFactory f, CancellationToken ct, string technician = Tech1, string tenant = TestUsers.TenantA)
    {
        var w = await f.RaiseAsync(ct, tenant);
        w = await f.Manager(tenant).ActAsync(w.Id, "approve", new { version = w.Version }, ct);
        return await f.Planner(tenant).ActAsync(w.Id, "schedule", new { version = w.Version, assignee = technician, scheduledFor = "2026-11-04" }, ct);
    }

    public static async Task<WorkOrderDetailModel> ActAsync(this HttpClient client, Guid id, string action, object body, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync($"/api/maintenance/work-orders/{id}/{action}", body, ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"{action}: {await response.Content.ReadAsStringAsync(ct)}");
        return (await response.Content.ReadFromJsonAsync<WorkOrderDetailModel>(ct))!;
    }

    /// <summary>A tiny PNG: only the signature matters to the server (it never decodes images).</summary>
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];

    public static MultipartFormDataContent FilePart(byte[] bytes, string fileName, string declaredType)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(declaredType);
        return new MultipartFormDataContent { { content, "file", fileName } };
    }
}
