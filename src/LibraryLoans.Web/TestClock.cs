namespace LibraryLoans.Web;

/// <summary>
/// A clock the tests can set through the Development-only /test/clock endpoint.
/// When no date is set it behaves like the system clock.
/// </summary>
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
