using System.Net.Http.Json;

namespace LibraryLoans.SystemTests;

public record Member(string Username, string Password, string Name);

public record Title(int Id, string Name);

/// <summary>Arranges system state through the app's /test endpoints.</summary>
public static class TestData
{
    private static readonly HttpClient Http = new() { BaseAddress = new Uri(LibraryTest.BaseUrl) };

    public static Task<Member> CreateMemberAsync(int loans = 0, decimal fines = 0) =>
        PostAsync<Member>("/test/members", new { loans, fines });

    public static Task<Title> CreateTitleAsync(string name, int copies = 1) =>
        PostAsync<Title>("/test/titles", new { name, copies });

    public static Task CreateLoanAsync(Member member, Title title, int dueInDays = 21) =>
        PostAsync<object>("/test/loans", new { username = member.Username, titleId = title.Id, dueInDays });

    private static async Task<T> PostAsync<T>(string path, object body)
    {
        var response = await Http.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
