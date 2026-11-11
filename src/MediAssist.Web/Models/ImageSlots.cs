namespace MediAssist.Web.Models;

/// <summary>
/// One image slot: where a photograph or illustration will go, at what size, and what it says
/// to screen readers. Files are dropped into wwwroot/images/{Key}.webp (or .jpg / .png); until then
/// the slot shows a finished-looking, wordless stand-in. See wwwroot/images/README.md.
/// </summary>
public sealed record ImageSlotSpec(
    string Key,
    string Ratio,          // CSS aspect-ratio, e.g. "3 / 2"
    string? MobileRatio,   // ratio under 48em, e.g. "4 / 3" (null = same)
    int Width,             // recommended delivery size at 2x
    int Height,
    string Alt,            // alt text once a real image is in place; "" = decorative
    string Focal = "50% 50%",
    string Kind = "photo", // "photo" or "illustration"
    string Note = "");

public static class ImageSlots
{
    private static readonly Dictionary<string, ImageSlotSpec> Specs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hero-patient-home"] = new("hero-patient-home", "3 / 2", "4 / 3", 2400, 1600,
            "A Filipino family talking with a health worker", "40% 50%",
            Note: "Families or caregivers in a real setting; subject left of centre; at most 250 KB as WebP."),
        ["hero-admin"] = new("hero-admin", "3 / 2", "4 / 3", 1800, 1200,
            "A MediAssist staff member reviewing paperwork at a desk",
            Note: "Review work; no readable patient data in frame."),
        ["hero-runner"] = new("hero-runner", "3 / 2", "4 / 3", 1800, 1200,
            "A MediAssist runner arriving at a hospital entrance with an envelope",
            Note: "An errand in progress; faces optional."),
        ["hero-programs"] = new("hero-programs", "3 / 2", "4 / 3", 2400, 1600,
            "A patient and family member speaking with a hospital social worker"),
        ["hero-documents"] = new("hero-documents", "3 / 2", "4 / 3", 2400, 1600,
            "Hands sorting printed forms and certificates into a folder",
            Note: "No readable names, ID numbers or medical details."),
        ["hero-notifications"] = new("hero-notifications", "3 / 2", "4 / 3", 2400, 1600,
            "Three generations of a family looking at a phone together"),
        ["hero-default"] = new("hero-default", "3 / 2", "4 / 3", 2400, 1600,
            "A family and a health worker at a community health centre",
            Note: "Used by any page whose own hero image is not supplied yet."),
        ["program-feature"] = new("program-feature", "4 / 3", null, 1600, 1200,
            "Patients waiting at a hospital billing and social service window",
            Note: "Per program: program-feature-{id}.webp overrides this default."),
        ["banner-notice"] = new("banner-notice", "16 / 9", null, 1920, 1080,
            "A health worker explaining a form to an older patient"),
        ["aside-runner"] = new("aside-runner", "1 / 1", null, 800, 800,
            "A runner handing an envelope to a hospital clerk"),
        ["aside-household"] = new("aside-household", "4 / 3", null, 1200, 900,
            "A family at home reviewing papers together"),
        ["notice-illustration"] = new("notice-illustration", "1 / 1", null, 480, 480,
            "", Kind: "illustration",
            Note: "Illustration (SVG or PNG): clipboard and clock, drawn in navy and gold. Decorative."),
        ["og-share"] = new("og-share", "1.91 / 1", null, 1200, 630,
            "MediAssist", Note: "Link-preview image for public pages (not used inside the portal yet)."),
    };

    /// <summary>
    /// Look up a slot. Unknown "hero-*" keys (section pages) inherit the hero-default size and alt;
    /// unknown "program-feature-*" keys inherit program-feature.
    /// </summary>
    public static ImageSlotSpec Get(string key)
    {
        if (Specs.TryGetValue(key, out var spec)) return spec;
        if (key.StartsWith("program-feature-", StringComparison.OrdinalIgnoreCase))
            return Specs["program-feature"] with { Key = key };
        return Specs["hero-default"] with { Key = key };
    }

    /// <summary>The slot's own file first, then its family default (hero-default, program-feature).</summary>
    public static IEnumerable<string> FileCandidates(string key)
    {
        yield return key;
        if (key.StartsWith("hero-", StringComparison.OrdinalIgnoreCase) &&
            !key.Equals("hero-default", StringComparison.OrdinalIgnoreCase))
            yield return "hero-default";
        if (key.StartsWith("program-feature-", StringComparison.OrdinalIgnoreCase))
            yield return "program-feature";
    }

    public static IReadOnlyCollection<ImageSlotSpec> All => Specs.Values;

    /// <summary>"hero-" + the path's segments, e.g. /patient/hospitals → hero-patient-hospitals.</summary>
    public static string HeroKeyForPath(string relativePath) =>
        "hero-" + string.Join('-', relativePath.Split('?', '#')[0].Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).Take(2));
}
