using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace LibraryLoans.SystemTests;

public class BorrowTests : PageTest
{
    [Test]
    public async Task Member_can_borrow_an_available_title()
    {
        await Page.GotoAsync("http://localhost:5080/login");
        await Page.GetByLabel("Username").FillAsync("alice");
        await Page.GetByLabel("Password").FillAsync("password1");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Page.GetByLabel("Search the catalog").FillAsync("Dune");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Dune" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Borrow" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Borrowed Dune");
    }
}
