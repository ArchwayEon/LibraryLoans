using Microsoft.Playwright;

namespace LibraryLoans.SystemTests;

public class BorrowTests : LibraryTest
{
    [Test]
    public async Task Member_can_borrow_an_available_title()   // ST-01: R1, R2
    {
        var member = await TestData.CreateMemberAsync();
        var title = await TestData.CreateTitleAsync("Dune");

        await LogInAsync(member);
        await OpenTitleAsync(title.Name);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Borrow" }).ClickAsync();

        var due = DateTime.Today.AddDays(21).ToString("yyyy-MM-dd");
        await Expect(Page.GetByRole(AriaRole.Status))
            .ToHaveTextAsync($"Borrowed {title.Name}. Due {due}.");
    }

    [Test]
    public async Task Member_at_the_loan_limit_cannot_borrow()   // ST-02: R3
    {
        var member = await TestData.CreateMemberAsync(loans: 5);
        var title = await TestData.CreateTitleAsync("Dune");

        await LogInAsync(member);
        await OpenTitleAsync(title.Name);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Borrow" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Loan limit reached");

        await Page.GetByRole(AriaRole.Link, new() { Name = "My loans" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Rowheader)).ToHaveCountAsync(5);
    }
}
