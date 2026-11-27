namespace SmartFactory.Modules.Maintenance.Domain;

/// <summary>
/// Identifies a file by its first bytes ("magic bytes"). Only the types on this allow-list are accepted.
/// Why: the file name and the declared Content-Type are chosen by the client and prove nothing.
/// </summary>
internal static class FileSignatures
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly (string ContentType, string Extension, byte[] Magic)[] Allowed =
    [
        ("image/jpeg", ".jpg", [0xFF, 0xD8, 0xFF]),
        ("image/png", ".png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        ("application/pdf", ".pdf", "%PDF-"u8.ToArray()),
    ];

    /// <summary>Bytes needed to recognise any allowed type.</summary>
    public static int HeaderLength { get; } = Allowed.Max(a => a.Magic.Length);

    public static IReadOnlyList<string> AllowedContentTypes { get; } = [.. Allowed.Select(a => a.ContentType)];

    /// <summary>Returns the detected content type and extension, or null if the header matches no allowed type.</summary>
    public static (string ContentType, string Extension)? Detect(ReadOnlySpan<byte> header)
    {
        foreach (var (contentType, extension, magic) in Allowed)
        {
            if (header.StartsWith(magic))
            {
                return (contentType, extension);
            }
        }

        return null;
    }

    /// <summary>A display-safe file name: no directories, no control characters, bounded length.</summary>
    public static string SafeDisplayName(string? fileName, string extension)
    {
        var name = Path.GetFileName(fileName ?? string.Empty);
        name = new string([.. name.Where(c => !char.IsControl(c) && c is not ('"' or '\\' or '/'))]).Trim();
        if (name.Length == 0)
        {
            name = "attachment" + extension;
        }

        return name.Length <= 120 ? name : name[..120];
    }
}
