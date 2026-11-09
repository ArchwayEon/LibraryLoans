# Implementing LL-12, Renew a Loan: An ATDD Walkthrough

The [acceptance test setup guide](../tests/LibraryLoans.AcceptanceTests/README.md) ended with three LL-12 scenarios that failed because the feature did not exist. This document picks up there and follows the **Develop** step of the ATDD cycle (*Automated Acceptance Testing* notes, section 6) until the librarian's examples pass.

The aim is not just the finished code. It is the **order** of the work, and what each failing run told us to do next. Every result quoted below is real output from running the tests at that stage.

The work is on the branch `ll-12-renew-loan`, one commit per stage:

```
29cafd0 LL-12: Add a settable clock for acceptance tests
1634fd9 LL-12: Renew a loan from the My loans page (AC1, AC4)
19e75fe LL-12: Specify AC2, AC3 and AC5 and add their test-support endpoints
34f8a88 LL-12: Refuse renewals at the limit, with a hold, or with fines (AC2, AC3, AC5)
```

Use `git show <commit>` to see exactly what changed at each step.

---

## The outer loop at a glance

| Stage | What we changed | Test result afterwards |
| --- | --- | --- |
| 0. Start | Nothing (the setup guide's state) | 3 tests, all fail at `Given today is …`: **404** |
| 1. Control time | Settable clock and `/test/clock` | All fail at `When Mia renews "Dune"`: **timeout**, no Renew button |
| 2. Smallest feature | `Library.Renew` for AC1 and AC4, Renew button | **3 of 3 pass** |
| 3a. Distill more examples | Scenarios for AC2, AC3 and AC5, with their bindings | 8 tests; 5 new ones fail at a new Given: **404** |
| 3b. Arrange the new state | Test-support endpoints for renewals, holds and fines | 3 refusal examples fail at `Then Mia is told …` |
| 4. Implement the rules | AC2, AC3 and AC5 in `Library.Renew` | **8 of 8 pass**; system tests 4 of 4 |

Read the last column from top to bottom. Each run fails **later in the scenario** than the one before: first in Given, then in When, then in Then, then not at all. That progression is how you know you are making real progress and not just changing things.

---

## Stage 0: Read the failure before you write code

```
Given today is 2 November 2026
-> error: Response status code does not indicate success: 404 (Not Found).
And Mia is a member
-> skipped because of previous errors
```

The notes warn that a failure in a **Given** step usually means a test-data problem, not a missing feature. Here it is both. The scenarios need to say "today is 2 November 2026", and the application has no way to be told what day it is. Controlling time is the first piece of LL-12 we must build, before any renewal logic.

---

## Stage 1: Make time controllable

**Commit `29cafd0`.**

### Why the clock matters

`Library` used `DateTime.Today` directly:

```csharp
public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
```

The test can't change that, so a scenario could never state concrete dates. The alternative, computing dates relative to the real today, makes the examples unreadable for the librarian ("due in 7 days, renewed 21 days later…"). The notes call concrete dates *worth the endpoint*, because anyone can check that 9 November + 21 days = 30 November.

### Step 1: Introduce a seam

.NET 8 and later provide `TimeProvider`, an abstract clock you can inject. `Library` now asks for one:

```csharp
private readonly TimeProvider _time;

public Library(TimeProvider time)
{
    _time = time;
    Reset();
}

public DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
```

Every rule that already used `Today` (borrowing, returning, adding loans) now follows the injected clock automatically. Nothing else in `Library` had to change.

### Step 2: A clock the tests can set

`src/LibraryLoans.Web/TestClock.cs`:

```csharp
public class TestClock : TimeProvider
{
    private readonly Lock _gate = new();
    private DateOnly? _today;

    /// <summary>The date to report as today, or null to use the real date.</summary>
    public DateOnly? Today
    {
        get { lock (_gate) return _today; }
        set { lock (_gate) _today = value; }
    }

    // Noon UTC keeps the local date the same in every time zone from UTC-11 to UTC+11.
    public override DateTimeOffset GetUtcNow() =>
        Today is { } date
            ? new DateTimeOffset(date.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero)
            : base.GetUtcNow();
}
```

When no date is set, it behaves exactly like the system clock. The lock matters because ASP.NET Core serves requests on many threads at once.

### Step 3: Register it in Development only

In `Program.cs`:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<TestClock>();
    builder.Services.AddSingleton<TimeProvider>(sp => sp.GetRequiredService<TestClock>());
}
else
{
    builder.Services.AddSingleton(TimeProvider.System);
}
```

A production build cannot have its clock changed, because there is no `TestClock` in it at all. The endpoints sit with the other test-support endpoints, inside `if (app.Environment.IsDevelopment())`:

```csharp
app.MapPost("/test/clock", (SetClock request, TestClock clock) =>
{
    clock.Today = request.Date;
    return new { request.Date };
});

app.MapDelete("/test/clock", (TestClock clock) => clock.Today = null);
```

### Step 4: Clean up after every scenario

The clock is **shared state in the application**, and context injection cannot isolate it, because it lives in the server, not in the test. If a scenario left it at 2 November 2026, every later test would run on that date. The system test `Member_can_borrow_an_available_title`, which expects a due date 21 days after the *real* today, would start failing for no visible reason.

So the test project gets an `[AfterScenario]` hook, `Hooks/ClockHooks.cs`:

```csharp
[Binding]
public class ClockHooks(LibraryWorld world, TestSupportClient support)
{
    [AfterScenario]
    public async Task ResetClock()
    {
        if (world.Today != default) await support.ResetClockAsync();
    }
}
```

`ResetClockAsync` sends `DELETE /test/clock`. An `[AfterScenario]` hook runs even when the scenario fails.

### Result

```
Given today is 2 November 2026
-> done: RenewalSteps.TodayIs(11/2/2026) (0.0s)
And Mia is a member
-> done: RenewalSteps.IsAMember("Mia") (0.0s)
Given Mia has "Dune" on loan, due 2 November 2026
-> done: RenewalSteps.HasOnLoan("Mia", "Dune", 11/2/2026) (0.0s)
When Mia renews "Dune"
-> error: Timeout 5000ms exceeded.
```

Every Given step now passes, and the scenarios fail at **When**, because there is no Renew button to click. This is "failing for the right reason": the test reaches the behavior LL-12 is about and finds it missing.

---

## Stage 2: Build the smallest thing that passes

**Commit `1634fd9`.**

Only two rules have scenarios so far: AC1 (extend by 21 days from the due date) and AC4 (overdue loans cannot be renewed). ATDD says to build what the examples demand and no more. The other rules come in stage 3, *with their examples first*.

### The domain rule

A loan needs to remember how many times it has been renewed (AC2 will use it), so `Loan` gains a field with a default value:

```csharp
public record Loan(int Id, string Username, int TitleId, DateOnly DueOn, int Renewals = 0);
```

`Library.Renew`, first version:

```csharp
public Result Renew(string username, int loanId)
{
    lock (_gate)
    {
        var index = _loans.FindIndex(l => l.Id == loanId && l.Username == username);
        if (index < 0) return new(false, "No open loan was found.");

        var loan = _loans[index];
        var title = _titles.First(t => t.Id == loan.TitleId);

        if (loan.DueOn < Today)                                         // AC4
            return new(false, "Overdue loans cannot be renewed.");

        var renewed = loan with                                          // AC1
        {
            DueOn = loan.DueOn.AddDays(LoanDays),
            Renewals = loan.Renewals + 1
        };
        _loans[index] = renewed;
        return new(true, $"Renewed {title.Name}. Due {renewed.DueOn:yyyy-MM-dd}.", renewed);
    }
}
```

Look at two details against the example map:

- `loan.DueOn.AddDays(LoanDays)`, not `Today.AddDays(LoanDays)`. This is the discriminating example at work: if you write the second, the "due 9 November, renewed 2 November" scenario fails with 23 November instead of 30 November.
- `loan.DueOn < Today`, not `<=`. The red card *"Is a loan due today overdue?"* was answered "no", and the boundary row "due 2 November, today 2 November: accepted" checks exactly that.

### The user interface

Each row on My loans gets a Renew button that posts a form:

```html
<td>
    <button type="button" data-action="return">Return</button>
    <form method="post" asp-page-handler="Renew" asp-route-loanId="@loan.Id" style="display:inline">
        <button type="submit">Renew</button>
    </form>
</td>
```

And the page handler:

```csharp
// Post/Redirect/Get: refreshing the page after a renewal must not renew again.
public IActionResult OnPostRenew(int loanId)
{
    TempData["Renewal"] = Library.Renew(User.Identity!.Name!, loanId).Message;
    return RedirectToPage();
}
```

`OnGet` shows the message with `Message ??= TempData["Renewal"] as string;`.

Two design points:

- **Post/Redirect/Get.** If the handler returned the page straight from the POST, pressing F5 would resubmit the form and renew the book a second time. Redirecting turns the result page into an ordinary GET. `TempData` carries the message across the redirect.
- **A bug in the existing code.** The page's script handled Return clicks with `if (event.target.tagName !== 'BUTTON') return;`, so it treated *every* button in the table as Return. The new Renew button would have quietly returned the book. The script now checks `button[data-action="return"]`. Adding a feature to existing code means reading what's already there; the system test `Member_can_return_book` confirms Return still works.

The page object written in the setup guide (`RenewAsync` clicks the row's **Renew** button and reads the `status` message) needed no changes. We wrote it for the UI we agreed on, then built that UI.

### Result

```
Passed OverdueBoundary("2 November 2026","23 November 2026","Renewed Dune.",...)
Passed OverdueBoundary("1 November 2026","1 November 2026","Overdue loans cannot be renewed.",...)
Passed RenewingMovesTheDueDateThreeWeeksLater
Total tests: 3
     Passed: 3
```

> **Where the inner loop goes.** In a full ATDD cycle, `Library.Renew` would be driven by unit tests (red, green, refactor) inside this outer loop. This repository has no unit test project, so this walkthrough relies on the acceptance scenarios alone. See exercise 1.

---

## Stage 3: Distill the rest of the example map

**Commit `19e75fe`.**

The example map in the notes has five rules. Three have no scenarios yet. Before writing any rule code, we add their examples to the feature file.

### 3a. New scenarios

```gherkin
  Rule: A loan can be renewed at most twice

    Scenario Outline: Renewal limit
      Given Mia has "Dune" on loan, due 9 November 2026
      And "Dune" has been renewed <renewals> before
      When Mia renews "Dune"
      Then Mia is told "<message>"
      And "Dune" is due on <new due date>

      Examples:
        | renewals | new due date     | message                              |
        | once     | 30 November 2026 | Renewed Dune.                        |
        | twice    | 9 November 2026  | Dune has already been renewed twice. |

  Rule: A loan cannot be renewed while another member has a hold on the title

    Scenario: Another member is waiting
      Given Mia has "Dune" on loan, due 9 November 2026
      And Omar has placed a hold on "Dune"
      When Mia renews "Dune"
      Then Mia is told "Another member is waiting for Dune."
      And "Dune" is due on 9 November 2026

  Rule: A member owing more than $10 in fines cannot renew

    Scenario Outline: Fine limit
      Given Mia has "Dune" on loan, due 9 November 2026
      And Mia owes $<fines> in fines
      When Mia renews "Dune"
      Then Mia is told "<message>"
      And "Dune" is due on <new due date>

      Examples:
        | fines | new due date     | message                                                    |
        | 10.00 | 30 November 2026 | Renewed Dune.                                              |
        | 10.01 | 9 November 2026  | You owe $10.01 in fines. Pay your fines before borrowing. |
```

That gives all **eight examples** from the map. Every refusal checks the message *and* that the due date did not move.

### New step definitions, organized by concept

The notes advise organizing steps by domain concept, not by feature file, so they can be reused. The new steps are:

| Step | Class | Notes |
| --- | --- | --- |
| `{string} has been renewed {word} before` | `RenewalSteps` | `once` and `twice` become 1 and 2 |
| `{word} has placed a hold on {string}` | `HoldSteps` | Creates Omar the first time he is mentioned |
| `{word} owes ${decimal} in fines` | `FineSteps` | `{decimal}` is built into Reqnroll |

```csharp
[Given("{string} has been renewed {word} before")]
public Task HasBeenRenewed(string title, string times) =>
    support.SetRenewalsAsync(world.Loans[title], times switch
    {
        "once" => 1,
        "twice" => 2,
        _ => throw new ArgumentException($"Expected 'once' or 'twice', not '{times}'.")
    });
```

To set the renewal count, the step needs to know which loan "Dune" means, so `HasOnLoan` now stores the loan the test-support endpoint returns in `world.Loans[title]`.

### The first run: failing in Given again

```
-> error: Response status code does not indicate success: 404 (Not Found).
  Failed AnotherMemberIsWaiting
  Failed FineLimit("10.00", …)
  Failed FineLimit("10.01", …)
  Failed RenewalLimit("once", …)
  Failed RenewalLimit("twice", …)
Total tests: 8
     Passed: 3
     Failed: 5
```

No step is *undefined*: every line found its binding. But the bindings call test-support endpoints that don't exist yet, so the scenarios cannot even set up their state. This is the "test-data problem" kind of Given failure the notes describe, and the fix belongs in the test-support layer, not the feature.

### 3b. Test-support endpoints

Three Development-only endpoints let a scenario arrange the state directly:

| Endpoint | Sets up |
| --- | --- |
| `POST /test/loans/{id}/renewals` | How many times a loan has been renewed |
| `POST /test/holds` | A hold by a member on a title |
| `POST /test/members/{username}/fines` | What a member owes |

Why not arrange this through the UI, for example by clicking Renew twice? Because that would make the AC2 example depend on renewal already working, and it would be slow. Given steps describe *state*; how that state is created is the automation's business.

### The second run: failing in Then

```
-> error:   Assert.That(world.LastMessage, Does.Contain(world.ToActualText(expected)))
  Expected: String containing "Dune 93fc7b has already been renewed twice."
  But was:  "Renewed Dune 93fc7b. Due 2026-11-30."
...
  Passed RenewalLimit("once", …)
  Failed RenewalLimit("twice", …)
Total tests: 8
     Passed: 5
     Failed: 3
```

Now the three refusal examples fail at **Then**: the app renews when it should refuse. That is the right reason.

Notice that the three *accepted* boundary rows (renewed once, owes $10.00) already pass. They're not wasted. They protect against implementing a rule too strictly. If you write `Fines >= FineLimit` instead of `Fines > FineLimit`, the $10.00 row fails.

---

## Stage 4: Implement the rules

**Commit `34f8a88`.**

```csharp
public const int MaxRenewals = 2;      // LL-12 AC2
```

```csharp
if (loan.DueOn < Today)                                         // AC4
    return new(false, "Overdue loans cannot be renewed.");

if (loan.Renewals >= MaxRenewals)                               // AC2
    return new(false, $"{title.Name} has already been renewed twice.");

// Any other member's hold blocks renewal, even if another copy is on the shelf.
// That case is an open question for the librarian.
if (_holds.Any(h => h.TitleId == title.Id && h.Username != username))   // AC3
    return new(false, $"Another member is waiting for {title.Name}.");

if (member.Fines > FineLimit)                                   // AC5, same limit and message as R4
    return new(false, FinesOwedMessage(member));
```

The example map recorded that AC5 must use **the same limit and message as borrowing rule R4**. Instead of copying the message, both `Borrow` and `Renew` now call one helper:

```csharp
private static string FinesOwedMessage(Member member) =>
    $"You owe ${member.Fines:0.00} in fines. Pay your fines before borrowing.";
```

If the librarian later changes the wording, it changes in one place, and the borrowing and renewal tests fail together. That is the "failing scenario may mean the specification changed" situation from section 5 of the notes.

### Result

```
Passed AnotherMemberIsWaiting
Passed FineLimit("10.00", …)
Passed FineLimit("10.01", …)
Passed OverdueBoundary("2 November 2026", …)
Passed OverdueBoundary("1 November 2026", …)
Passed RenewalLimit("once", …)
Passed RenewalLimit("twice", …)
Passed RenewingMovesTheDueDateThreeWeeksLater
Total tests: 8
     Passed: 8
```

And the system tests, which check that borrowing, returning and searching still work:

```
Passed Member_at_the_loan_limit_cannot_borrow
Passed Member_can_borrow_an_available_title
Passed Member_can_return_book
Passed Search_shows_matching_titles
Total tests: 4
     Passed: 4
```

---

## Decisions we had to make, and who should confirm them

Passing scenarios do not mean the story is *accepted*. Several decisions were made by the developer and must go back to the librarian at the **Demo** step:

| Decision | What the code does now | Why it needs the product owner |
| --- | --- | --- |
| Which message wins when several rules refuse? | Checks run in the order overdue, limit, hold, fines; the first one wins | Open red card from the example map |
| A hold exists but another copy is on the shelf | The hold still blocks renewal | Open red card; the librarian is checking the lending policy |
| Wording for AC2 and AC3 | "Dune has already been renewed twice." and "Another member is waiting for Dune." | Invented by the team; the librarian never saw them |
| AC5 message says "before borrowing" | Reused word for word from R4 | Agreed in the map, but it reads oddly for a renewal |

None of the eight examples combines two refusal reasons, so the tests do not pin down the order. If the librarian chooses an order, add an example that combines two reasons to lock it in.

---

## A caution about shared state

The clock, holds and fines live in **one running application** shared by every test. Context injection isolates the test side, not the server. Two practical consequences:

1. **Run the two test projects one after the other, not in parallel, against the same server.** `dotnet test LibraryLoans.slnx` runs the projects in parallel. A system test that expects the real date could then run while an acceptance scenario has the clock set to 2 November 2026. Our run passed, but only because of timing. Run the projects separately, or give each suite its own app instance:

   ```bash
   dotnet test tests/LibraryLoans.AcceptanceTests
   dotnet test tests/LibraryLoans.SystemTests
   ```

2. **Data isolation still comes from unique names.** Each scenario creates its own member and a title with a random suffix, so Omar's hold on `Dune 3fa2c1` cannot affect another scenario's `Dune 9b04e7`.

---

## Done means accepted

By the definition in the notes, LL-12 is **not done yet**. The scenarios pass and there are no `@wip` tags, but the librarian has not seen the demo or answered the open questions above. What the passing scenarios give the demo is time: nobody will spend the librarian's half hour on a rule that is already broken. The demo can focus on whether renewing fits the way members actually use the site.

---

## Exercises

1. **Add the inner loop.** Create a `LibraryLoans.UnitTests` NUnit project and write unit tests for `Library.Renew`. Use a fake `TimeProvider` (or `TestClock`) so the tests control the date without HTTP. Which examples belong at this level rather than in the feature file? (Hint: the test pyramid, notes section 3.)
2. **Lock the precedence.** Suppose the librarian decides that an overdue loan should always report "Overdue" first, even if fines are also owed. Write the example, watch it pass or fail, and explain why.
3. **Answer the shelf question.** Suppose the librarian rules that a hold does *not* block renewal when another copy is available. Write the example first, watch it fail, then change `Renew`.
4. **Show renewals used.** The notes suggest `Then "Dune" shows 1 of 2 renewals used`. Specify it, then build it.
5. **Remove the race.** Change the test setup so the system tests and the acceptance tests can never share a clock. Compare at least two approaches, such as separate app instances, a per-request clock header, or running in sequence.
