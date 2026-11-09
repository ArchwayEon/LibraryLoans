# Automating Acceptance Tests with Reqnroll and Playwright

This guide walks you through building the `LibraryLoans.AcceptanceTests` project: an executable specification for story **LL-12, Renew a loan**. It follows the *Automated Acceptance Testing* notes (sections 3 and 4) and assumes you have already worked through the system tests in `LibraryLoans.SystemTests`.

By the end you will have:

- a feature file written in Gherkin that the librarian could read,
- step definitions that turn each step into one or two calls,
- an automation layer (page objects and a test-support client) that knows how the application works,
- hooks that start and stop the browser,
- a test run that **fails for the right reason**, because renewal has not been built yet.

---

## 1. Before you start

You need:

| Tool | Check with |
| --- | --- |
| .NET 10 SDK | `dotnet --version` |
| PowerShell 7 (for the Playwright install script) | `pwsh --version` |
| The LibraryLoans solution, building cleanly | `dotnet build LibraryLoans.slnx` |

All commands below are run from the **repository root** (the folder that holds `LibraryLoans.slnx`) unless a step says otherwise.

---

## 2. Create the project

A Reqnroll project is an ordinary NUnit test project with two extra packages.

```bash
dotnet new nunit -n LibraryLoans.AcceptanceTests -o tests/LibraryLoans.AcceptanceTests --framework net10.0
cd tests/LibraryLoans.AcceptanceTests
dotnet add package Reqnroll.NUnit
dotnet add package Microsoft.Playwright.NUnit --version 1.63.0
cd ../..
dotnet sln LibraryLoans.slnx add tests/LibraryLoans.AcceptanceTests/LibraryLoans.AcceptanceTests.csproj
```

Then delete the template's `UnitTest1.cs`; you will not need it.

What the packages do:

- **Reqnroll.NUnit** brings in Reqnroll and a build-time generator. Every `.feature` file is turned into an NUnit test class when you build, so each scenario (and each row of an `Examples` table) appears as its own test.
- **Microsoft.Playwright.NUnit** brings in Playwright. The notes use plain `Microsoft.Playwright`; the NUnit variant includes it and also gives us Playwright's `Assertions.Expect`. We pin version 1.63.0 so it matches the system tests.

> **Note:** Unlike the system tests, our classes do **not** inherit from Playwright's `PageTest`. Reqnroll owns the test classes, so we start the browser ourselves in hooks (section 8).

### Ignore the generated code

The generator writes a `*.feature.cs` file next to each feature file on every build. It is generated output, so keep it out of source control by adding this to `.gitignore`:

```gitignore
# Reqnroll generated code-behind
*.feature.cs
```

### Install the browser

Build once, then install Chromium for Playwright (skip this if you already did it for the system tests):

```bash
dotnet build tests/LibraryLoans.AcceptanceTests
pwsh tests/LibraryLoans.AcceptanceTests/bin/Debug/net10.0/playwright.ps1 install chromium
```

---

## 3. Project layout

Create these folders. Each one is a layer from Figure 1 of the notes, and each changes for a different reason.

```
tests/LibraryLoans.AcceptanceTests/
├── Features/              ← the specification (changes when a business rule changes)
│   └── RenewLoan.feature
├── Steps/                 ← thin glue from step text to code
│   ├── RenewalSteps.cs
│   └── Transforms.cs
├── Hooks/                 ← start/stop the browser
│   └── BrowserHooks.cs
├── Pages/                 ← page objects (change when the UI changes)
│   ├── LibraryApp.cs
│   └── MyLoansPage.cs
└── Support/               ← test-support client and per-scenario state
    ├── LibraryWorld.cs
    └── TestSupportClient.cs
```

The rule to keep in mind: **selectors live only in `Pages/`, HTTP details live only in `Support/`, and step definitions contain neither.**

---

## 4. Write the feature file

Start with the specification. This is what you would agree with the librarian before writing any code.

`Features/RenewLoan.feature`:

```gherkin
Feature: Renew a loan (LL-12)
  As a member, I want to renew a book I have on loan,
  so that I can keep reading it without visiting the library.

  Background:
    Given today is 2 November 2026
    And Mia is a member

  Rule: A renewal extends the due date by 21 days from the current due date

    Scenario: Renewing moves the due date three weeks later
      Given Mia has "Dune" on loan, due 9 November 2026
      When Mia renews "Dune"
      Then Mia is told "Renewed Dune."
      And "Dune" is due on 30 November 2026

  Rule: An overdue loan cannot be renewed

    Scenario Outline: Overdue boundary
      Given Mia has "Dune" on loan, due <due date>
      When Mia renews "Dune"
      Then Mia is told "<message>"
      And "Dune" is due on <new due date>

      Examples:
        | due date        | new due date     | message                          |
        | 2 November 2026 | 23 November 2026 | Renewed Dune.                    |
        | 1 November 2026 | 1 November 2026  | Overdue loans cannot be renewed. |
```

Things to notice:

- **Declarative style.** No step mentions a button, a URL, or a field. Ask yourself: would this still be true if renewals were done by phone? It would.
- **Concrete dates.** Anyone can check that 9 November + 21 days is 30 November. That is why the Background fixes today's date.
- **Discriminating example.** Renewing on 2 November separates "21 days from today" (23 November) from "21 days from the due date" (30 November).
- **Boundaries.** The outline tests a loan due *today* (accepted) and due *yesterday* (refused).
- **Refusals check two things.** The second outline row checks the message *and* that the due date did not change.

Build now and run `dotnet test tests/LibraryLoans.AcceptanceTests`. Every step is reported as **undefined**. That is expected: there are no bindings yet. An undefined step is never reported as a pass.

---

## 5. Per-scenario state: `LibraryWorld`

Steps in one scenario need to share data: Mia's login, the title that was created, the message from the last action. Reqnroll's **context injection** creates one instance of any class named in a binding's constructor *per scenario* and hands the same instance to every binding that asks for it.

`Support/LibraryWorld.cs`:

```csharp
namespace LibraryLoans.AcceptanceTests.Support;

/// <summary>Per-scenario state shared by step definitions through context injection.</summary>
public class LibraryWorld
{
    /// <summary>The date the scenario set with "today is".</summary>
    public DateOnly Today { get; set; }

    /// <summary>Members by the name used in the feature file, such as "Mia".</summary>
    public Dictionary<string, TestMember> Members { get; } = new();

    /// <summary>Titles by the name used in the feature file, such as "Dune".</summary>
    public Dictionary<string, TestTitle> Titles { get; } = new();

    public string? LastMessage { get; set; }

    /// <summary>
    /// Replaces each feature-file title name with the unique name the app stored,
    /// so that "Renewed Dune." matches "Renewed Dune 3fa2c1.".
    /// </summary>
    public string ToActualText(string text)
    {
        foreach (var (name, title) in Titles)
            text = text.Replace(name, title.Name);
        return text;
    }
}
```

Why the dictionaries? The test-support endpoint adds a random suffix to every title (for example `Dune 3fa2c1`) so tests stay isolated, just as in the system tests. The feature file says "Dune"; the world remembers which real title that means.

> **Do not use static fields for scenario data.** They survive from one scenario to the next and bring back the order dependence that context injection removes.

---

## 6. The automation layer

### Test-support client

Given steps arrange state through the app's Development-only `/test` endpoints instead of clicking through the UI.

`Support/TestSupportClient.cs`:

```csharp
using System.Net.Http.Json;
using LibraryLoans.AcceptanceTests.Pages;

namespace LibraryLoans.AcceptanceTests.Support;

public record TestMember(string Username, string Password, string Name);

public record TestTitle(int Id, string Name);

/// <summary>Arranges system state through the app's Development-only /test endpoints.</summary>
public class TestSupportClient
{
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(LibraryApp.BaseUrl) };

    public Task<TestMember> CreateMemberAsync() =>
        PostAsync<TestMember>("/test/members", new { loans = 0, fines = 0 });

    public Task<TestTitle> CreateTitleAsync(string name) =>
        PostAsync<TestTitle>("/test/titles", new { name, copies = 1 });

    public Task CreateLoanAsync(TestMember member, TestTitle title, int dueInDays) =>
        PostAsync<object>("/test/loans", new { username = member.Username, titleId = title.Id, dueInDays });

    /// <summary>Sets the application's clock. Not yet provided by the app; LL-12 adds it.</summary>
    public Task SetClockAsync(DateOnly date) =>
        PostAsync<object>("/test/clock", new { date = date.ToString("yyyy-MM-dd") });

    private static async Task<T> PostAsync<T>(string path, object body)
    {
        var response = await Http.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
```

The `HttpClient` is `static` because it is safe to share and expensive to create. It holds no scenario data, so it does not break isolation.

### Page objects

`Pages/LibraryApp.cs` is the entry point to the UI:

```csharp
using LibraryLoans.AcceptanceTests.Support;
using Microsoft.Playwright;

namespace LibraryLoans.AcceptanceTests.Pages;

/// <summary>Entry point to the application's pages. The automation layer, not the steps, knows the UI.</summary>
public class LibraryApp(IPage page)
{
    public const string BaseUrl = "http://localhost:5080";

    public async Task<MyLoansPage> LogInAsync(TestMember member)
    {
        await page.GotoAsync("/login");
        await page.GetByLabel("Username").FillAsync(member.Username);
        await page.GetByLabel("Password").FillAsync(member.Password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
        return await MyLoansAsync();
    }

    public Task<MyLoansPage> MyLoansAsync() => MyLoansPage.OpenAsync(page);
}
```

`Pages/MyLoansPage.cs`:

```csharp
using Microsoft.Playwright;

namespace LibraryLoans.AcceptanceTests.Pages;

public class MyLoansPage(IPage page)
{
    public ILocator Status => page.GetByRole(AriaRole.Status);

    public static async Task<MyLoansPage> OpenAsync(IPage page)
    {
        await page.GotoAsync("/loans");
        return new MyLoansPage(page);
    }

    /// <summary>Clicks Renew on the row for the title and returns the status message the app shows.</summary>
    public async Task<string> RenewAsync(string title)
    {
        await Row(title).GetByRole(AriaRole.Button, new() { Name = "Renew" }).ClickAsync();
        await Assertions.Expect(Status).Not.ToBeEmptyAsync();
        return (await Status.TextContentAsync())!;
    }

    public async Task<DateOnly> DueDateAsync(string title)
    {
        var text = await Row(title).Locator("time").GetAttributeAsync("datetime");
        return DateOnly.ParseExact(text!, "yyyy-MM-dd");
    }

    private ILocator Row(string title) =>
        page.GetByRole(AriaRole.Row).Filter(new() { HasText = title });
}
```

Points to notice:

- Locators use **roles and labels**, not CSS ids, exactly as in the system tests.
- `RenewAsync` waits for the status message with `Expect(...).Not.ToBeEmptyAsync()` instead of a fixed delay. Playwright retries until the text appears or the timeout expires.
- The Renew button does not exist yet. You are writing the page object for the UI you *expect*; it is part of the agreement about what will be built.

---

## 7. Step definitions

### The date transformation

`DateOnly` is not a built-in parameter type, so tell Reqnroll how to turn "9 November 2026" into a `DateOnly`. Every step that uses `{DateOnly}` then gets the conversion for free.

`Steps/Transforms.cs`:

```csharp
using System.Globalization;
using Reqnroll;

namespace LibraryLoans.AcceptanceTests.Steps;

[Binding]
public class Transforms
{
    [StepArgumentTransformation(@"(\d{1,2} \w+ \d{4})")]
    public DateOnly ToDate(string text) =>
        DateOnly.ParseExact(text, "d MMMM yyyy", CultureInfo.InvariantCulture);
}
```

### The steps

`Steps/RenewalSteps.cs`:

```csharp
using LibraryLoans.AcceptanceTests.Pages;
using LibraryLoans.AcceptanceTests.Support;
using Reqnroll;

namespace LibraryLoans.AcceptanceTests.Steps;

[Binding]
public class RenewalSteps(LibraryWorld world, TestSupportClient support, LibraryApp app)
{
    [Given("today is {DateOnly}")]
    public async Task TodayIs(DateOnly date)
    {
        world.Today = date;
        await support.SetClockAsync(date);
    }

    [Given("{word} is a member")]
    public async Task IsAMember(string name) =>
        world.Members[name] = await support.CreateMemberAsync();

    [Given("{word} has {string} on loan, due {DateOnly}")]
    public async Task HasOnLoan(string member, string title, DateOnly due)
    {
        world.Titles[title] = await support.CreateTitleAsync(title);
        await support.CreateLoanAsync(world.Members[member], world.Titles[title], due.DayNumber - world.Today.DayNumber);
    }

    [When("{word} renews {string}")]
    public async Task Renews(string member, string title)
    {
        var loans = await app.LogInAsync(world.Members[member]);
        world.LastMessage = await loans.RenewAsync(world.Titles[title].Name);
    }

    [Then("{word} is told {string}")]
    public Task IsTold(string member, string expected)
    {
        Assert.That(world.LastMessage, Does.Contain(world.ToActualText(expected)));
        return Task.CompletedTask;
    }

    [Then("{string} is due on {DateOnly}")]
    public async Task IsDueOn(string title, DateOnly expected)
    {
        var loans = await app.MyLoansAsync();
        var actual = await loans.DueDateAsync(world.Titles[title].Name);
        Assert.That(actual, Is.EqualTo(expected));
    }
}
```

How this works:

- **Steps match by text.** `[Given("{word} has {string} on loan, due {DateOnly}")]` is a *Cucumber expression*. `{word}` matches one word (`Mia`), `{string}` matches quoted text (`"Dune"`, without the quotes), and `{DateOnly}` uses our transformation.
- **Constructor injection.** Reqnroll creates `LibraryWorld`, `TestSupportClient`, and `LibraryApp` for each scenario. `LibraryApp` needs an `IPage`, which the browser hook registers (next section).
- **Each method is a line or two.** If a step definition starts to contain selectors, URLs, or loops, that code belongs in the automation layer.
- **Step definitions are global.** Any feature file in the project can use any step. Before writing a new step, search for an existing one that says the same thing.
- **The loan's due date is relative.** The `/test/loans` endpoint takes a number of days from the app's today, so `HasOnLoan` subtracts the scenario's `Today` from the due date.

---

## 8. Hooks: starting the browser

One browser for the whole run keeps the suite fast. A fresh browser context for each scenario means no cookies or logins leak between scenarios.

`Hooks/BrowserHooks.cs`:

```csharp
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
```

- **Run-level hooks (`[BeforeTestRun]`, `[AfterTestRun]`) must be `static`.**
- `container.RegisterInstanceAs(page)` puts the page into the scenario's container. That is how `LibraryApp` receives it through its constructor.
- The 5-second default timeout makes a missing element fail quickly instead of waiting Playwright's default 30 seconds.
- To watch the browser while debugging, launch with `new() { Headless = false, SlowMo = 500 }`.

---

## 9. Run the tests

The tests drive the real application, so start it first, in a separate terminal, on the port the tests expect:

```bash
dotnet run --project src/LibraryLoans.Web --urls http://localhost:5080
```

The default launch profile runs in the **Development** environment, which is the only environment where the `/test` endpoints exist.

Then, in another terminal:

```bash
dotnet test tests/LibraryLoans.AcceptanceTests --logger "console;verbosity=normal"
```

You can also run them from the Test Explorer in Visual Studio or VS Code. You should see three tests: one scenario plus one per row of the outline.

### What you should see

All three tests fail, and all of them fail on the same step:

```
Given today is 2 November 2026
-> error: Response status code does not indicate success: 404 (Not Found).
And Mia is a member
-> skipped because of previous errors
...
```

**This is the correct result at this stage.** You have finished the *Distill* step of the ATDD cycle and the bindings for *Develop*. The scenarios fail because the feature does not exist yet:

1. The app has no `/test/clock` endpoint, so the scenario cannot fix today's date.
2. The My loans page has no **Renew** button.
3. The `Library` class has no renewal rules.

As the notes put it, a scenario should **fail for the right reason**. Undefined steps mean missing bindings. A failure in the Given steps usually means a test-data problem. Here the Given step fails because the clock endpoint is part of LL-12 and has not been built. Once that endpoint exists, the scenarios should move on and fail at the When step (no Renew button), which is where you want them to fail before building the feature.

---

## 10. Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| Step reported as *undefined* | No step definition matches the text exactly | Compare the step text with the expression; check spacing and quotes |
| *Ambiguous step definitions* | Two expressions match the same step | Make one expression more specific, or merge them |
| `HttpRequestException: Connection refused` | The app is not running, or is on another port | Start the app with `--urls http://localhost:5080` |
| `404` from a `/test/...` endpoint | The app is not in Development, or the endpoint does not exist yet | Check the environment; for `/test/clock`, see section 9 |
| `Executable doesn't exist` from Playwright | Browser not installed | Run the `playwright.ps1 install chromium` command from section 2 |
| Timeout clicking a button | Element missing or named differently | Run headed (section 8) and compare with the page; fix the page object, not the step |
| Changes to a `.feature` file not picked up | Generated code is stale | Rebuild the project |

---

## 11. Exercises

1. **Implement LL-12 until the scenarios pass.**
   - Add a Development-only `POST /test/clock` endpoint. Give the app a `TimeProvider` through dependency injection, and use it in place of `DateTime.Today` in `Library`.
   - Add a **Renew** button to each row on the My loans page.
   - Add the renewal rules to `Library`.

   Run the scenarios after each change and watch them fail at later and later steps until they pass.
2. **Automate the rest of the example map.** The notes list five rules and eight examples. Add rules AC2 (at most two renewals), AC3 (a hold blocks renewal), and AC5 (fines over $10). You will need new Given steps, such as `"Dune" has been renewed twice before` and `Omar has placed a hold on "Dune"`, and the test-support endpoints behind them. Reuse existing steps wherever the wording allows.
3. **Tag the scenarios.** Add `@LL-12` to the feature, and `@wip` to any scenario not yet implemented. Then run only the finished ones with `dotnet test --filter "TestCategory!=wip"`.
4. **Bind one level down.** Choose one rule example and bind its When step to an HTTP call instead of the browser (a *subcutaneous* test). The feature file must not change.
