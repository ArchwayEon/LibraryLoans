using LibraryLoans.SystemTests.Pages;

namespace LibraryLoans.SystemTests;

public class SearchTests : LibraryTest
{
    [Test]
    public async Task Search_shows_matching_titles()
    {
        var member = await TestData.CreateMemberAsync();
        var title = await TestData.CreateTitleAsync("Emma");

        var catalog = await new LoginPage(Page).LogInAsync(member);
        await catalog.SearchAsync(title.Name);

        await Expect(catalog.Status).ToHaveTextAsync("1 title found.");
        await Expect(catalog.ResultLinks).ToHaveTextAsync(title.Name);
    }
}
