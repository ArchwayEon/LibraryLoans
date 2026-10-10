using Microsoft.Playwright;

namespace LibraryLoans.SystemTests;

public class SearchTests : LibraryTest
{
    [Test]
    public async Task Search_shows_matching_titles()
    {
        var member = await TestData.CreateMemberAsync();
        var title = await TestData.CreateTitleAsync("Emma");

        await LogInAsync(member);
        await Page.GetByLabel("Search the catalog").FillAsync(title.Name);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("1 title found.");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = title.Name })).ToBeVisibleAsync();
    }
}
