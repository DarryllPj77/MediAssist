using MediAssist.Web.Models;

namespace MediAssist.Web.Tests;

/// <summary>Form validation rules (M3.3): account, patient profile, authorization basis, runner request, simulated files.</summary>
public class ValidationRulesTests
{
    private static readonly DateOnly Today = new(2026, 11, 25);

    [Theory]
    [InlineData("Maria", "Santos", null, true)]
    [InlineData("Maria", "Santos", "maria@example.com", true)]
    [InlineData("", "Santos", null, false)]
    [InlineData("Maria", "123", null, false)]
    [InlineData("Maria", "Santos", "not-an-email", false)]
    public void Account_details(string first, string last, string? email, bool valid) =>
        Assert.Equal(valid, AccountRules.Validate(first, last, email).Count == 0);

    [Fact]
    public void Account_names_have_a_length_limit() =>
        Assert.True(AccountRules.Validate(new string('a', AccountRules.NameMax + 1), "Santos", null).ContainsKey("first"));

    [Fact]
    public void A_family_member_needs_a_name_relationship_and_a_past_date_of_birth()
    {
        Assert.Empty(ProfileRules.Validate("Ana Reyes", "Daughter", new DateOnly(2015, 3, 10), null, isSelf: false, Today));
        var errors = ProfileRules.Validate("", "Neighbour", Today.AddDays(1), new string('x', 61), isSelf: false, Today);
        Assert.Equal(new[] { "city", "dob", "name", "relationship" }, errors.Keys.Order().ToArray());
    }

    [Fact]
    public void The_account_holder_needs_no_relationship() =>
        Assert.Empty(ProfileRules.Validate("Maria Santos", null, new DateOnly(1984, 6, 9), null, isSelf: true, Today));

    [Fact]
    public void A_parents_basis_fits_only_a_patient_under_18()
    {
        var child = new PatientProfile(Guid.NewGuid(), "Ana Reyes", "Daughter", AuthorizationStatus.NotRequested, new DateOnly(2015, 3, 10));
        var adult = child with { DateOfBirth = new DateOnly(1990, 1, 1) };
        Assert.Null(ProfileRules.BasisProblem(AuthorizationBasis.ParentOfMinor, child, Today));
        Assert.NotNull(ProfileRules.BasisProblem(AuthorizationBasis.ParentOfMinor, adult, Today));
        Assert.Null(ProfileRules.BasisProblem(AuthorizationBasis.SignedLetter, adult, Today));
        Assert.NotNull(ProfileRules.BasisProblem(null, adult, Today));
    }

    private static AssistanceApplication Application(ApplicationStatus status) =>
        new("MA-2026-00418", Guid.NewGuid(), "Jose Santos", "San Isidro General Hospital", "Hospital Bill Support",
            status, 3, 5, DateTimeOffset.Now, "", "hospital-bill", DateTimeOffset.Now);

    [Fact]
    public void A_runner_request_needs_an_open_application_a_date_in_range_and_confirmation()
    {
        var ok = RunnerRules.Validate(Application(ApplicationStatus.UnderReview), false, RunnerErrand.DeliverDocuments,
            Today.AddDays(2), RunnerRules.Windows[0], null, true, Today);
        Assert.Empty(ok);

        Assert.Contains("application", RunnerRules.Validate(Application(ApplicationStatus.Completed), false, RunnerErrand.DeliverDocuments,
            Today.AddDays(2), RunnerRules.Windows[0], null, true, Today).Keys);
        Assert.Contains("application", RunnerRules.Validate(Application(ApplicationStatus.UnderReview), true, RunnerErrand.DeliverDocuments,
            Today.AddDays(2), RunnerRules.Windows[0], null, true, Today).Keys);

        var bad = RunnerRules.Validate(Application(ApplicationStatus.UnderReview), false, null,
            Today, "Evening", new string('n', RunnerRules.NotesMaxLength + 1), false, Today);
        Assert.Equal(new[] { "confirm", "date", "errand", "notes", "window" }, bad.Keys.Order().ToArray());
        Assert.Contains("date", RunnerRules.Validate(Application(ApplicationStatus.UnderReview), false, RunnerErrand.DeliverDocuments,
            Today.AddDays(RunnerRules.LatestDays + 1), RunnerRules.Windows[0], null, true, Today).Keys);
    }

    [Fact]
    public void Simulated_files_must_be_pdf_jpg_or_png_up_to_10_MB()
    {
        var samples = SimulatedFiles.SamplesFor("Valid ID");
        Assert.Null(SimulatedFiles.Validate(samples[0]));      // PDF scan
        Assert.Null(SimulatedFiles.Validate(samples[1]));      // JPG photo
        Assert.NotNull(SimulatedFiles.Validate(samples[2]));   // HEIC
        Assert.NotNull(SimulatedFiles.Validate(samples[3]));   // 18.4 MB
        Assert.NotNull(SimulatedFiles.Validate(null));
    }
}
