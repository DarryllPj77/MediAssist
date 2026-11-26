namespace MediAssist.Web.E2E;

/// <summary>Sign-in and access by role in the real app (M3.4).</summary>
[Collection("app")]
public sealed class AccessTests(AppFixture app)
{
    [Theory]
    [InlineData("/")]
    [InlineData("/patient/applications")]
    [InlineData("/admin/reviews")]
    [InlineData("/runner/tasks")]
    public async Task Signed_out_visitors_are_sent_to_sign_in_and_brought_back(string route)
    {
        var page = await app.PageAsync(null);
        await page.GotoAsync(app.BaseUrl + route);

        Assert.Contains("/account/login", page.Url);
        Assert.Contains(Uri.EscapeDataString(route), page.Url);
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("maria.santos@example.com", "/patient")]
    [InlineData("ana.reyes@example.com", "/admin")]
    [InlineData("liza.manalo@example.com", "/runner")]
    public async Task Each_account_lands_in_its_own_portal(string email, string home)
    {
        var page = await app.PageAsync(email);
        Assert.EndsWith(home, new Uri(page.Url).AbsolutePath);
        await page.Context.CloseAsync();
    }

    [Theory]
    [InlineData("maria.santos@example.com", "/admin/reviews")]
    [InlineData("maria.santos@example.com", "/runner/tasks")]
    [InlineData("ana.reyes@example.com", "/patient/applications")]
    [InlineData("paolo.dizon@example.com", "/admin/authorizations")]
    public async Task Another_roles_portal_is_denied(string email, string route)
    {
        var page = await app.PageAsync(email);
        await page.GotoAsync(app.BaseUrl + route);

        Assert.Contains("/account/access-denied", page.Url);
        Assert.Equal("This page isn’t for your account", await page.InnerTextAsync("h1"));
        await page.Context.CloseAsync();
    }

    [Fact]
    public async Task A_runner_sees_only_their_own_errands()
    {
        var page = await app.PageAsync("ramil.ocampo@example.com");
        await page.GotoAsync(app.BaseUrl + "/runner/tasks/rr-29"); // Paolo's errand
        await page.WaitForTimeoutAsync(1000);

        Assert.Equal("This errand isn’t yours", await page.InnerTextAsync("h1"));
        await page.Context.CloseAsync();
    }

    [Fact]
    public async Task Signing_out_ends_access()
    {
        var page = await app.PageAsync("ana.reyes@example.com");
        await page.WaitForTimeoutAsync(1000); // let the live connection start
        await page.ClickAsync("form[action='/account/logout'] button[type=submit] >> visible=true");
        await page.WaitForURLAsync(url => url.Contains("/account/login", StringComparison.OrdinalIgnoreCase));

        await page.GotoAsync(app.BaseUrl + "/admin");
        Assert.Contains("/account/login", page.Url);
        await page.Context.CloseAsync();
    }
}
