using Microsoft.Playwright;

namespace LibraryLoans.SystemTests.Pages;

public class LoginPage(IPage page)
{
    public async Task<CatalogPage> LogInAsync(Member member)
    {
        await page.GotoAsync("/login");
        await page.GetByLabel("Username").FillAsync(member.Username);
        await page.GetByLabel("Password").FillAsync(member.Password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        return new CatalogPage(page);
    }
}
