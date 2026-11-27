namespace SmartFactory.Platform.Api;

/// <summary>
/// The container health check: <c>dotnet SmartFactory.Platform.Api.dll --health-probe</c> calls /health/live on the
/// instance already running in the same container and turns the answer into an exit code (0 healthy, 1 not).
/// </summary>
internal static class HealthProbe
{
    public static async Task<int> RunAsync()
    {
        // ASPNETCORE_HTTP_PORTS may list several ports ("8080;8081"); the first one is the HTTP listener.
        var port = (Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080").Split(';', ',')[0].Trim();
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            using var response = await http.GetAsync(new Uri($"http://127.0.0.1:{port}/health/live"));
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            await Console.Error.WriteLineAsync($"health probe failed: {ex.Message}");
            return 1;
        }
    }
}
