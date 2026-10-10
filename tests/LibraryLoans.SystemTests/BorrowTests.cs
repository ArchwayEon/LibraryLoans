using LibraryLoans.SystemTests.Pages;

namespace LibraryLoans.SystemTests;

public class BorrowTests : LibraryTest
{
    [Test]
    public async Task Member_can_borrow_an_available_title()   // ST-01: R1, R2
    {
        var member = await TestData.CreateMemberAsync();
        var title = await TestData.CreateTitleAsync("Dune");

        var catalog = await new LoginPage(Page).LogInAsync(member);
        var titlePage = await catalog.OpenTitleAsync(title.Name);
        var loans = await titlePage.BorrowAsync();

        await Expect(loans.Status).ToContainTextAsync($"Borrowed {title.Name}");
        Assert.That(await loans.DueDateAsync(title.Name),
            Is.EqualTo(DateOnly.FromDateTime(DateTime.Today).AddDays(21)));
    }

    [Test]
    public async Task Member_at_the_loan_limit_cannot_borrow()   // ST-02: R3
    {
        var member = await TestData.CreateMemberAsync(loans: 5);
        var title = await TestData.CreateTitleAsync("Dune");

        var catalog = await new LoginPage(Page).LogInAsync(member);
        var titlePage = await catalog.OpenTitleAsync(title.Name);
        await titlePage.BorrowExpectingRefusalAsync();

        await Expect(titlePage.Error).ToContainTextAsync("Loan limit reached");

        var loans = await MyLoansPage.OpenAsync(Page);
        await Expect(loans.LoanTitles).ToHaveCountAsync(5);
    }
}
