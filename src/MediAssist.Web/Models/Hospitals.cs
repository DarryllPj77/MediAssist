namespace MediAssist.Web.Models;

// Hospital directory (Milestone 2, Phase D5). Every hospital, district and office hour is invented.

/// <summary>A hospital in the directory and the assistance programs its social service office handles.</summary>
public sealed record Hospital(
    string Id,
    string Name,
    string Type,
    string District,
    string City,
    IReadOnlyList<string> Services,
    IReadOnlyList<string> ProgramIds,
    string SocialServiceHours,
    string SocialServiceLocation)
{
    /// <summary>The hospital's page in the patient portal.</summary>
    public string Href => $"/patient/hospitals/{Id}";

    public bool Offers(string? programId) => programId is not null && ProgramIds.Contains(programId);

    /// <summary>True when every word of <paramref name="query"/> appears in the name, type, place or services.</summary>
    public bool Matches(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        var haystack = $"{Name} {Type} {District} {City} {string.Join(' ', Services)}";
        return query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .All(word => haystack.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
