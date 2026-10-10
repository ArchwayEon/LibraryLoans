using Microsoft.Playwright;

namespace LibraryLoans.SystemTests.Pages;

public class CatalogPage(IPage page)
{
    public ILocator Status => page.GetByRole(AriaRole.Status);

    public ILocator ResultLinks =>
        page.GetByRole(AriaRole.List, new() { Name = "Search results" }).GetByRole(AriaRole.Link);

    public async Task SearchAsync(string text)
    {
        await page.GetByLabel("Search the catalog").FillAsync(text);
        await page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
    }

    public async Task<TitlePage> OpenTitleAsync(string title)
    {
        await SearchAsync(title);
        await page.GetByRole(AriaRole.Link, new() { Name = title }).ClickAsync();
        return new TitlePage(page);
    }
}
