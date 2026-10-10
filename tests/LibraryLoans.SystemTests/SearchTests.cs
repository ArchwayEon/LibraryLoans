using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace LibraryLoans.SystemTests;

public class SearchTests : PageTest
{
    [SetUp]
    public async Task LogIn()
    {
        await Page.GotoAsync("http://localhost:5080/login");
        await Page.GetByLabel("Username").FillAsync("bob");
        await Page.GetByLabel("Password").FillAsync("password2");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
    }

    [Test]
    public async Task Search_brittle()
    {
        await Page.FillAsync("#q", "Emma");
        await Page.ClickAsync("#search-form button");

        var count = await Page.Locator("#results li").CountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task Search_robust()
    {
        await Page.GetByLabel("Search the catalog").FillAsync("Emma");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

        var results = Page.GetByRole(AriaRole.List, new() { Name = "Search results" })
                          .GetByRole(AriaRole.Listitem);
        await Expect(results).ToHaveCountAsync(1);
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Emma" })).ToBeVisibleAsync();
    }
}
