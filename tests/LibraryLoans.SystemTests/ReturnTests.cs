using LibraryLoans.SystemTests.Pages;

namespace LibraryLoans.SystemTests;

public class ReturnTests : LibraryTest
{
    [Test]
    public async Task Member_can_return_book()
    {
        var member = await TestData.CreateMemberAsync();
        var title = await TestData.CreateTitleAsync("Dune");
        await TestData.CreateLoanAsync(member, title);

        await new LoginPage(Page).LogInAsync(member);
        var loans = await MyLoansPage.OpenAsync(Page);
        await loans.ReturnAsync(title.Name);

        await Expect(loans.Status).ToHaveTextAsync($"Returned {title.Name}.");
        await Expect(loans.LoanTitles).ToHaveCountAsync(0);
    }
}
