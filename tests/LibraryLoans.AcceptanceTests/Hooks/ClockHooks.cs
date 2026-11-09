using LibraryLoans.AcceptanceTests.Support;
using Reqnroll;

namespace LibraryLoans.AcceptanceTests.Hooks;

[Binding]
public class ClockHooks(LibraryWorld world, TestSupportClient support)
{
    /// <summary>Puts the app back on the real date, so no later test runs on the scenario's date.</summary>
    [AfterScenario]
    public async Task ResetClock()
    {
        if (world.Today != default) await support.ResetClockAsync();
    }
}
