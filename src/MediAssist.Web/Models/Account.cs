using System.Text.RegularExpressions;

namespace MediAssist.Web.Models;

/// <summary>
/// The account holder's own details as they changed them in Settings (D8), kept in their browser.
/// Overrides the seeded account; the "You" patient profile carries the same name.
/// </summary>
public sealed record AccountDetails(string FirstName, string LastName, string? Email);

/// <summary>Validation for the account form (D8). Keys: "first", "last", "email".</summary>
public static partial class AccountRules
{
    public const int NameMax = 40;
    public const int EmailMax = 254;

    public static Dictionary<string, string> Validate(string? firstName, string? lastName, string? email)
    {
        var errors = new Dictionary<string, string>();
        NameError(firstName, "first", "first name", errors);
        NameError(lastName, "last", "last name", errors);

        var e = email?.Trim() ?? "";
        if (e.Length > EmailMax) errors["email"] = $"Enter an email address of {EmailMax} characters or fewer.";
        else if (e.Length > 0 && !EmailShape().IsMatch(e)) errors["email"] = "Enter an email address like name@example.com.";
        return errors;
    }

    private static void NameError(string? value, string key, string label, Dictionary<string, string> errors)
    {
        var v = value?.Trim() ?? "";
        if (v.Length == 0) errors[key] = $"Enter your {label}.";
        else if (v.Length > NameMax) errors[key] = $"Enter a {label} of {NameMax} characters or fewer.";
        else if (!v.Any(char.IsLetter)) errors[key] = $"Enter a {label} using letters.";
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailShape();
}

/// <summary>Whether the sidebar follows the screen size or stays expanded or compact (saved per browser).</summary>
public enum SidebarPreference { Auto, Expanded, Collapsed }
