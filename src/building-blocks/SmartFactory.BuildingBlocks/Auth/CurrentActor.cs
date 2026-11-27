using Microsoft.AspNetCore.Http;

namespace SmartFactory.BuildingBlocks.Auth;

/// <summary>Who is performing the current action; recorded in every audit event.</summary>
public interface ICurrentActor
{
    /// <summary>Stable identifier: the token's <c>sub</c>, or a <c>system:*</c> name for non-interactive work.</summary>
    string Id { get; }

    /// <summary>Human-readable name: <c>preferred_username</c>, or the system actor name.</summary>
    string DisplayName { get; }
}

/// <summary>Names a non-interactive actor (seeding, background jobs) for the duration of a scope.</summary>
public static class ActorScope
{
    private static readonly AsyncLocal<string?> Override = new();

    internal static string? Current => Override.Value;

    public static IDisposable Begin(string systemActor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemActor);
        var previous = Override.Value;
        Override.Value = systemActor.StartsWith("system:", StringComparison.Ordinal) ? systemActor : $"system:{systemActor}";
        return new Restore(() => Override.Value = previous);
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}

internal sealed class CurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    public string Id =>
        ActorScope.Current
        ?? accessor.HttpContext?.User.FindFirst("sub")?.Value
        ?? "system:unknown";

    public string DisplayName =>
        ActorScope.Current
        ?? accessor.HttpContext?.User.Identity?.Name
        ?? accessor.HttpContext?.User.FindFirst("sub")?.Value
        ?? "system:unknown";
}
