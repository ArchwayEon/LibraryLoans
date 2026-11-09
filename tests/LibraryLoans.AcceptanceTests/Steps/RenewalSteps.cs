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
        world.Loans[title] = await support.CreateLoanAsync(
            world.Members[member], world.Titles[title], due.DayNumber - world.Today.DayNumber);
    }

    [Given("{string} has been renewed {word} before")]
    public Task HasBeenRenewed(string title, string times) =>
        support.SetRenewalsAsync(world.Loans[title], times switch
        {
            "once" => 1,
            "twice" => 2,
            _ => throw new ArgumentException($"Expected 'once' or 'twice', not '{times}'.")
        });

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
