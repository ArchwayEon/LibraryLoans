using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace LibraryLoans.AcceptanceTests.Pages;

public class MyLoansPage(IPage page)
{
    public ILocator Status => page.GetByRole(AriaRole.Status);

    public static async Task<MyLoansPage> OpenAsync(IPage page)
    {
        await page.GotoAsync("/loans");
        return new MyLoansPage(page);
    }

    /// <summary>Clicks Renew on the row for the title and returns the status message the app shows.</summary>
    public async Task<string> RenewAsync(string title)
    {
        await Row(title).GetByRole(AriaRole.Button, new() { Name = "Renew" }).ClickAsync();
        await Assertions.Expect(Status).Not.ToBeEmptyAsync();
        return (await Status.TextContentAsync())!;
    }

    public async Task<DateOnly> DueDateAsync(string title)
    {
        var text = await Row(title).Locator("time").GetAttributeAsync("datetime");
        return DateOnly.ParseExact(text!, "yyyy-MM-dd");
    }

    private ILocator Row(string title) =>
        page.GetByRole(AriaRole.Row).Filter(new() { HasText = title });
}
