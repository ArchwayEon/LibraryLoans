using LibraryLoans.AcceptanceTests.Support;
using Microsoft.Playwright;

namespace LibraryLoans.AcceptanceTests.Pages;

/// <summary>Entry point to the application's pages. The automation layer, not the steps, knows the UI.</summary>
public class LibraryApp(IPage page)
{
    public const string BaseUrl = "http://localhost:5080";

    public async Task<MyLoansPage> LogInAsync(TestMember member)
    {
        await page.GotoAsync("/login");
        await page.GetByLabel("Username").FillAsync(member.Username);
        await page.GetByLabel("Password").FillAsync(member.Password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        return await MyLoansAsync();
    }

    public Task<MyLoansPage> MyLoansAsync() => MyLoansPage.OpenAsync(page);
}
