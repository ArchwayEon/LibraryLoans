using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace LibraryLoans.SystemTests;

public abstract class LibraryTest : PageTest
{
    public const string BaseUrl = "http://localhost:5080";

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = BaseUrl };

    [SetUp]
    public void UseShortTimeouts() => Page.SetDefaultTimeout(5_000);

    protected async Task LogInAsync(Member member)
    {
        await Page.GotoAsync("/login");
        await Page.GetByLabel("Username").FillAsync(member.Username);
        await Page.GetByLabel("Password").FillAsync(member.Password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
    }

    protected async Task OpenTitleAsync(string title)
    {
        await Page.GetByLabel("Search the catalog").FillAsync(title);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync();
    }
}
