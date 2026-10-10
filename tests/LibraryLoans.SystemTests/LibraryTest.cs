using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework.Interfaces;

namespace LibraryLoans.SystemTests;

public abstract class LibraryTest : PageTest
{
    public const string BaseUrl = "http://localhost:5080";

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = BaseUrl };

    [SetUp]
    public async Task StartTrace()
    {
        Page.SetDefaultTimeout(5_000);

        await Context.Tracing.StartAsync(new()
        {
            Title = TestContext.CurrentContext.Test.Name,
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    [TearDown]
    public async Task SaveTraceIfFailed()
    {
        var failed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed;

        await Context.Tracing.StopAsync(new()
        {
            Path = failed
                ? Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-traces",
                               $"{TestContext.CurrentContext.Test.Name}.zip")
                : null
        });
    }
}
