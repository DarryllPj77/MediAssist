namespace MediAssist.Web.Models;

// Simulated document uploads (Milestone 2, Phase D3). No file is read from the user's device:
// the user picks one of a few fictional sample files, so no real medical record or file name is
// ever stored. Uploads are kept per user in the browser (ApplicationWorkspace).

/// <summary>What a reviewer decided about an upload. Set by administrator reviews (D9); pending until then.</summary>
public enum UploadReviewState
{
    Pending,
    Accepted,
    Rejected
}

/// <summary>
/// One uploaded version of one required document. A replacement is a new record with the next
/// <see cref="Version"/> and <see cref="ReplacesId"/> pointing at the one it replaces; older
/// versions stay for the history.
/// </summary>
public sealed record DocumentUpload(
    string Id,
    string ApplicationReference,
    string DocumentName,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt,
    int Version,
    string? ReplacesId = null,
    UploadReviewState ReviewState = UploadReviewState.Pending,
    string? ReviewNote = null);

/// <summary>A fictional file offered by the simulated picker.</summary>
public sealed record SampleFile(string Id, string FileName, string ContentType, long SizeBytes);

/// <summary>The simulated picker's files and the upload rules.</summary>
public static class SimulatedFiles
{
    public const long MaxBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> AcceptedTypes = new()
    {
        ["application/pdf"] = "PDF",
        ["image/jpeg"] = "JPG",
        ["image/png"] = "PNG",
    };

    /// <summary>"PDF, JPG or PNG, up to 10 MB" for hints.</summary>
    public static string RulesText => "PDF, JPG or PNG, up to 10 MB";

    /// <summary>
    /// Four sample files for a document: two valid (a scan and a photo) and two that show the
    /// rules (a HEIC photo and an oversized scan).
    /// </summary>
    public static IReadOnlyList<SampleFile> SamplesFor(string documentName)
    {
        var slug = string.Join('-', documentName.ToLowerInvariant()
            .Split([' ', '\'', '’', '/'], StringSplitOptions.RemoveEmptyEntries));
        return
        [
            new($"{slug}-scan", $"{slug}-scan.pdf", "application/pdf", 1_258_291),
            new($"{slug}-photo", $"{slug}-photo.jpg", "image/jpeg", 2_516_582),
            new($"{slug}-heic", $"{slug}-photo.heic", "image/heic", 3_250_586),
            new($"{slug}-large", $"{slug}-full-scan.pdf", "application/pdf", 19_293_798),
        ];
    }

    public static string TypeLabel(string contentType) =>
        AcceptedTypes.TryGetValue(contentType, out var label) ? label
        : contentType.StartsWith("image/", StringComparison.Ordinal) ? contentType["image/".Length..].ToUpperInvariant()
        : contentType;

    /// <summary>Why a file cannot be uploaded, or null when it can.</summary>
    public static string? Validate(SampleFile? file) =>
        file is null ? "Choose a file to upload."
        : !AcceptedTypes.ContainsKey(file.ContentType) ? $"{file.FileName} is a {TypeLabel(file.ContentType)} file. Upload a PDF, JPG or PNG."
        : file.SizeBytes > MaxBytes ? $"{file.FileName} is {FormatSize(file.SizeBytes)}. Upload a file of 10 MB or less."
        : null;

    /// <summary>"1.2 MB", "820 KB".</summary>
    public static string FormatSize(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / (1024d * 1024):0.#} MB" : $"{Math.Max(1, bytes / 1024)} KB";
}
