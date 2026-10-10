using Microsoft.Playwright;

namespace LibraryLoans.SystemTests.Pages;

public class MyLoansPage(IPage page)
{
    public ILocator Status => page.GetByRole(AriaRole.Status);

    public ILocator LoanTitles => page.GetByRole(AriaRole.Rowheader);

    public static async Task<MyLoansPage> OpenAsync(IPage page)
    {
        await page.GotoAsync("/loans");
        return new MyLoansPage(page);
    }

    public async Task<DateOnly> DueDateAsync(string title)
    {
        var text = await Row(title).Locator("time").GetAttributeAsync("datetime");
        return DateOnly.ParseExact(text!, "yyyy-MM-dd");
    }

    public async Task ReturnAsync(string title) =>
        await Row(title).GetByRole(AriaRole.Button, new() { Name = "Return" }).ClickAsync();

    private ILocator Row(string title) =>
        page.GetByRole(AriaRole.Row).Filter(new() { HasText = title });
}
