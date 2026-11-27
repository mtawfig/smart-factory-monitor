using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SmartFactory.BuildingBlocks;

/// <summary>
/// Every HTTP response carries a header stating that the data is synthetic and the system advisory.
/// Why: the content rules require the label on every surface, and a header makes it true for API
/// consumers (and visible in browser dev tools) without touching any payload.
/// </summary>
public static class SyntheticDataHeaderMiddleware
{
    public const string HeaderName = "X-SmartFactory-Data";
    public const string HeaderValue = "synthetic; advisory-only; no-plc-control";

    public static IApplicationBuilder UseSyntheticDataHeader(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(static state =>
            {
                var response = (HttpResponse)state;
                response.Headers[HeaderName] = HeaderValue;
                return Task.CompletedTask;
            }, context.Response);
            await next(context).ConfigureAwait(false);
        });
    }
}
