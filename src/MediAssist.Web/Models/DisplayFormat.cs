using System.Globalization;

namespace MediAssist.Web.Models;

/// <summary>Human-friendly date formatting shared by dashboard components.</summary>
public static class DisplayFormat
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-PH");

    /// <summary>"Today, 2:40 PM", "Yesterday", "3 days ago", or "21 Oct 2026".</summary>
    public static string Relative(DateTimeOffset when, DateTimeOffset now)
    {
        var days = (now.Date - when.Date).Days;
        return days switch
        {
            <= 0 => $"Today, {when.ToString("h:mm tt", Culture)}",
            1 => "Yesterday",
            < 7 => $"{days} days ago",
            _ => when.ToString("d MMM yyyy", Culture)
        };
    }

    /// <summary>"Wednesday, 11 November".</summary>
    public static string LongDay(DateTimeOffset when) => when.ToString("dddd, d MMMM", Culture);

    public static string ShortDate(DateTimeOffset when) => when.ToString("d MMM", Culture);

    public static string Greeting(DateTimeOffset now) => now.Hour switch
    {
        < 12 => "Good morning",
        < 18 => "Good afternoon",
        _ => "Good evening"
    };

    /// <summary>ISO 8601 for the &lt;time datetime&gt; attribute.</summary>
    public static string Machine(DateTimeOffset when) => when.ToString("yyyy-MM-ddTHH:mmzzz", CultureInfo.InvariantCulture);
}
