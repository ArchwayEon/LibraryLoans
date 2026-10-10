using Microsoft.Playwright;

namespace LibraryLoans.SystemTests.Pages;

public class TitlePage(IPage page)
{
    public ILocator Error => page.GetByRole(AriaRole.Alert);

    /// <summary>Borrows the title. On success the app shows the My loans page.</summary>
    public async Task<MyLoansPage> BorrowAsync()
    {
        await ClickBorrowAsync();
        return new MyLoansPage(page);
    }

    /// <summary>Tries to borrow when the app should refuse. The member stays on this page.</summary>
    public async Task BorrowExpectingRefusalAsync() => await ClickBorrowAsync();

    private async Task ClickBorrowAsync() =>
        await page.GetByRole(AriaRole.Button, new() { Name = "Borrow" }).ClickAsync();
}
