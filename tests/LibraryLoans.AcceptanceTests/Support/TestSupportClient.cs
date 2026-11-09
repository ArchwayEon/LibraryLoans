using System.Net.Http.Json;
using LibraryLoans.AcceptanceTests.Pages;

namespace LibraryLoans.AcceptanceTests.Support;

public record TestMember(string Username, string Password, string Name);

public record TestTitle(int Id, string Name);

public record TestLoan(int Id);

/// <summary>Arranges system state through the app's Development-only /test endpoints.</summary>
public class TestSupportClient
{
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(LibraryApp.BaseUrl) };

    public Task<TestMember> CreateMemberAsync() =>
        PostAsync<TestMember>("/test/members", new { loans = 0, fines = 0 });

    public Task<TestTitle> CreateTitleAsync(string name) =>
        PostAsync<TestTitle>("/test/titles", new { name, copies = 1 });

    public Task<TestLoan> CreateLoanAsync(TestMember member, TestTitle title, int dueInDays) =>
        PostAsync<TestLoan>("/test/loans", new { username = member.Username, titleId = title.Id, dueInDays });

    public Task SetRenewalsAsync(TestLoan loan, int renewals) =>
        PostAsync<object>($"/test/loans/{loan.Id}/renewals", new { renewals });

    public Task PlaceHoldAsync(TestMember member, TestTitle title) =>
        PostAsync<object>("/test/holds", new { username = member.Username, titleId = title.Id });

    public Task SetFinesAsync(TestMember member, decimal fines) =>
        PostAsync<object>($"/test/members/{member.Username}/fines", new { fines });

    /// <summary>Sets the date the application treats as today.</summary>
    public Task SetClockAsync(DateOnly date) =>
        PostAsync<object>("/test/clock", new { date = date.ToString("yyyy-MM-dd") });

    /// <summary>Returns the application to the real date.</summary>
    public async Task ResetClockAsync() =>
        (await Http.DeleteAsync("/test/clock")).EnsureSuccessStatusCode();

    private static async Task<T> PostAsync<T>(string path, object body)
    {
        var response = await Http.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
