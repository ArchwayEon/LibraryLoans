using LibraryLoans.AcceptanceTests.Pages;
using Microsoft.Playwright;
using Reqnroll;
using Reqnroll.BoDi;

namespace LibraryLoans.AcceptanceTests.Hooks;

[Binding]
public class BrowserHooks(IObjectContainer container)
{
    private static IPlaywright playwright = null!;
    private static IBrowser browser = null!;
    private IBrowserContext? context;

    [BeforeTestRun]
    public static async Task StartBrowser()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync();
    }

    [BeforeScenario]
    public async Task OpenPage()
    {
        context = await browser.NewContextAsync(new() { BaseURL = LibraryApp.BaseUrl });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(5_000);
        container.RegisterInstanceAs(page);
    }

    [AfterScenario]
    public async Task ClosePage()
    {
        if (context is not null) await context.CloseAsync();
    }

    [AfterTestRun]
    public static async Task StopBrowser()
    {
        await browser.CloseAsync();
        playwright.Dispose();
    }
}
