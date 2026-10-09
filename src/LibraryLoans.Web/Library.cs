namespace LibraryLoans.Web;

public record Member(string Username, string Password, string Name)
{
    public decimal Fines { get; set; }
}

public record Title(int Id, string Name, string Author, int Copies);

public record Loan(int Id, string Username, int TitleId, DateOnly DueOn);

public record Result(bool Ok, string Message, Loan? Loan = null);

public class Library
{
    public const int LoanLimit = 5;        // R3
    public const int LoanDays = 21;        // R2
    public const decimal FineLimit = 10m;  // R4
    public const decimal FinePerDay = 0.25m;

    private readonly Lock _gate = new();
    private readonly List<Member> _members = [];
    private readonly List<Title> _titles = [];
    private readonly List<Loan> _loans = [];
    private readonly List<(string Username, int TitleId)> _holds = [];
    private int _nextId;

    public Library() => Reset();

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public void Reset()
    {
        lock (_gate)
        {
            _members.Clear(); _titles.Clear(); _loans.Clear(); _holds.Clear();
            _nextId = 0;
            _members.Add(new Member("alice", "password1", "Alice"));
            _members.Add(new Member("bob", "password2", "Bob"));
            AddTitle("Dune", "Frank Herbert", 1);
            AddTitle("Emma", "Jane Austen", 2);
            AddTitle("Neuromancer", "William Gibson", 1);
            AddTitle("Beloved", "Toni Morrison", 1);
            AddTitle("The Hobbit", "J. R. R. Tolkien", 3);
        }
    }

    // ----- queries -----

    public Member? FindMember(string username, string password)
    {
        lock (_gate) return _members.FirstOrDefault(m => m.Username == username && m.Password == password);
    }

    public Member GetMember(string username)
    {
        lock (_gate) return _members.First(m => m.Username == username);
    }

    public List<Title> Search(string? text)
    {
        lock (_gate)
            return _titles
                .Where(t => string.IsNullOrWhiteSpace(text)
                         || t.Name.Contains(text.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Name)
                .ToList();
    }

    public Title? GetTitle(int id)
    {
        lock (_gate) return _titles.FirstOrDefault(t => t.Id == id);
    }

    public int AvailableCopies(int titleId)
    {
        lock (_gate)
        {
            var title = _titles.First(t => t.Id == titleId);
            return title.Copies - _loans.Count(l => l.TitleId == titleId);
        }
    }

    public List<(Loan Loan, Title Title)> LoansFor(string username)
    {
        lock (_gate)
            return _loans
                .Where(l => l.Username == username)
                .Select(l => (l, _titles.First(t => t.Id == l.TitleId)))
                .OrderBy(x => x.Item2.Name)
                .ToList();
    }

    // ----- commands -----

    public Result Borrow(string username, int titleId)
    {
        lock (_gate)
        {
            var member = _members.First(m => m.Username == username);
            var title = _titles.First(t => t.Id == titleId);

            if (_loans.Count(l => l.Username == username) >= LoanLimit)
                return new(false, $"Loan limit reached: you may hold at most {LoanLimit} loans.");
            if (member.Fines > FineLimit)
                return new(false, $"You owe ${member.Fines:0.00} in fines. Pay your fines before borrowing.");
            if (AvailableCopies(titleId) <= 0)
                return new(false, "No copies are available.");

            var loan = new Loan(++_nextId, username, titleId, Today.AddDays(LoanDays));
            _loans.Add(loan);
            return new(true, $"Borrowed {title.Name}. Due {loan.DueOn:yyyy-MM-dd}.", loan);
        }
    }

    public Result Return(string username, int loanId)
    {
        lock (_gate)
        {
            var loan = _loans.FirstOrDefault(l => l.Id == loanId && l.Username == username);
            if (loan is null) return new(false, "No open loan was found.");

            var member = _members.First(m => m.Username == username);
            var title = _titles.First(t => t.Id == loan.TitleId);
            _loans.Remove(loan);

            var daysLate = Today.DayNumber - loan.DueOn.DayNumber;
            if (daysLate <= 0) return new(true, $"Returned {title.Name}.");

            var fine = daysLate * FinePerDay;
            member.Fines += fine;
            return new(true, $"Returned {title.Name}. Overdue by {daysLate} days: fine ${fine:0.00}.");
        }
    }

    public Result PlaceHold(string username, int titleId)
    {
        lock (_gate)
        {
            var title = _titles.First(t => t.Id == titleId);
            if (_holds.Contains((username, titleId)))
                return new(false, $"You already have a hold on {title.Name}.");
            _holds.Add((username, titleId));
            return new(true, $"Hold placed on {title.Name}.");
        }
    }

    // ----- used by the seed data and by the /test endpoints -----

    public Title AddTitle(string name, string author, int copies)
    {
        lock (_gate)
        {
            var title = new Title(++_nextId, name, author, copies);
            _titles.Add(title);
            return title;
        }
    }

    public Member AddMember(decimal fines)
    {
        lock (_gate)
        {
            var tag = Guid.NewGuid().ToString("N")[..6];
            var member = new Member($"member-{tag}", "secret", $"Member {tag}") { Fines = fines };
            _members.Add(member);
            return member;
        }
    }

    public Loan AddLoan(string username, int titleId, int dueInDays)
    {
        lock (_gate)
        {
            var loan = new Loan(++_nextId, username, titleId, Today.AddDays(dueInDays));
            _loans.Add(loan);
            return loan;
        }
    }
}
