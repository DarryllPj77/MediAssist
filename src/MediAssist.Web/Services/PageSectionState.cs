namespace MediAssist.Web.Services;

/// <summary>
/// Lets a page that has no navigation entry (404, error) name its own section in the top bar.
/// Set through the <c>SectionLabel</c> component; cleared when that page goes away.
/// </summary>
public sealed class PageSectionState
{
    public string? Label { get; private set; }

    public event Action? Changed;

    public void Set(string? label)
    {
        if (Label == label) return;
        Label = label;
        Changed?.Invoke();
    }
}
