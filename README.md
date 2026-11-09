# LibraryLoans

LibraryLoans is a small library web app for **CSCI 5220**, used to practise automated system testing and **acceptance test-driven development (ATDD)**. Members can log in, search the catalogue, borrow and return books, and renew loans. The app is kept deliberately small so you can focus on the tests, not the app.

The repository goes with the course notes, *Automated Acceptance Testing* ([CSCI5220-AutomatedAcceptanceTesting.docx](CSCI5220-AutomatedAcceptanceTesting.docx)).

## What's in the repository

| Path | What it is |
| --- | --- |
| [src/LibraryLoans.Web](src/LibraryLoans.Web) | The app: ASP.NET Core Razor Pages with in-memory data. In Development it also has `/test/...` endpoints the tests use to set up data and the clock. |
| [tests/LibraryLoans.SystemTests](tests/LibraryLoans.SystemTests) | Playwright + NUnit system tests for searching, borrowing and returning books. |
| [tests/LibraryLoans.AcceptanceTests](tests/LibraryLoans.AcceptanceTests) | Reqnroll (Gherkin) + Playwright acceptance tests for story **LL-12, Renew a loan**. Its [README](tests/LibraryLoans.AcceptanceTests/README.md) shows how to build this project step by step. |
| [docs/LL-12-Implementation.md](docs/LL-12-Implementation.md) | A walkthrough of implementing LL-12 test-first, from failing scenarios to passing ones. |

## Prerequisites

| Tool | Check with |
| --- | --- |
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | `dotnet --version` |
| PowerShell 7 (to install the Playwright browser) | `pwsh --version` |
| Git | `git --version` |

Any editor works. Visual Studio, VS Code with C# Dev Kit, and Rider can all open `LibraryLoans.slnx` and run the tests from their test explorer.

## Setup

Run all commands from the repository root (the folder that contains `LibraryLoans.slnx`).

1. Clone the repository and build the solution:

   ```bash
   git clone <repository-url>
   cd LibraryLoans
   dotnet build LibraryLoans.slnx
   ```

2. Install Chromium for Playwright. You only need to do this once per machine:

   ```bash
   pwsh tests/LibraryLoans.SystemTests/bin/Debug/net10.0/playwright.ps1 install chromium
   ```

## Run the app

```bash
dotnet run --project src/LibraryLoans.Web --urls http://localhost:5080
```

Open <http://localhost:5080> and log in with one of the seeded accounts:

| Username | Password |
| --- | --- |
| `alice` | `password1` |
| `bob` | `password2` |

All data is held in memory, so it resets every time the app restarts.

> **Use port 5080.** The tests expect the app at `http://localhost:5080`. If you start it with plain `dotnet run` or F5, it uses the launch profile's port (5073) and the tests can't reach it.

## Run the tests

The tests drive the real app in a browser, so keep the app running (see above) in its own terminal. Then, in a second terminal, run each test project separately:

```bash
dotnet test tests/LibraryLoans.AcceptanceTests
dotnet test tests/LibraryLoans.SystemTests
```

Don't run them together with `dotnet test LibraryLoans.slnx`. That runs both projects in parallel against the same app. The acceptance tests set the app's clock to a fixed date, which can make the system tests fail at random. [LL-12-Implementation.md](docs/LL-12-Implementation.md#a-caution-about-shared-state) explains why.

Useful options:

- **See each step as it runs:** add `--logger "console;verbosity=normal"`.
- **Watch the system tests in a browser:** add `--settings tests/LibraryLoans.SystemTests/headed.runsettings`.
- **Debug a failed system test:** open its Playwright trace, saved under `playwright-traces/` in the test output folder, with `pwsh tests/LibraryLoans.SystemTests/bin/Debug/net10.0/playwright.ps1 show-trace <file>.zip`.

The `/test/...` endpoints exist only in the **Development** environment, which `dotnet run` uses by default. If the tests get `404` errors from `/test/...`, check that the app is running in Development.

## Where to go next

1. **[tests/LibraryLoans.AcceptanceTests/README.md](tests/LibraryLoans.AcceptanceTests/README.md)** shows how to build the acceptance test project: the feature file, step definitions, page objects and hooks. It ends with scenarios that fail because LL-12 hasn't been built yet.
2. **[docs/LL-12-Implementation.md](docs/LL-12-Implementation.md)** continues from there. It implements LL-12 one stage at a time and explains what each failing run tells you to do next, until all eight scenarios pass. Each stage is its own commit on the `ll-12-renew-loan` branch, so you can step through them:

   ```bash
   git log --oneline 9322f5a..ll-12-renew-loan   # the LL-12 commits, oldest at the bottom
   git show <commit>
   ```
