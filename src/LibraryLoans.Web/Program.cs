using System.Security.Claims;
using LibraryLoans.Web;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// The Development build gets a clock the tests can set; every other build uses the real clock.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<TestClock>();
    builder.Services.AddSingleton<TimeProvider>(sp => sp.GetRequiredService<TestClock>());
}
else
{
    builder.Services.AddSingleton(TimeProvider.System);
}

builder.Services.AddSingleton<Library>();
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options => options.LoginPath = "/login");
builder.Services.AddAuthorization();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.MapPost("/logout", async (HttpContext http) =>
{
    await http.SignOutAsync("Cookies");
    return Results.Redirect("/login");
});

// ----- JSON endpoints called by the pages' JavaScript -----
// The short delays stand in for a slow network, so the pages update asynchronously.

app.MapGet("/api/titles", async (string? q, Library library) =>
{
    await Task.Delay(600);
    return library.Search(q).Select(t => new { t.Id, t.Name, t.Author });
}).RequireAuthorization();

app.MapPost("/api/loans/{id:int}/return", async (int id, ClaimsPrincipal user, Library library) =>
{
    await Task.Delay(600);
    var result = library.Return(user.Identity!.Name!, id);
    var fines = library.GetMember(user.Identity!.Name!).Fines;
    return new { result.Ok, result.Message, Fines = fines.ToString("0.00") };
}).RequireAuthorization();

// ----- Test-support endpoints: arrange system state without driving the UI -----
// Mapped only in the Development environment. They must never ship to production.

if (app.Environment.IsDevelopment())
{
    app.MapPost("/test/reset", (Library library) => library.Reset());

    app.MapPost("/test/clock", (SetClock request, TestClock clock) =>
    {
        clock.Today = request.Date;
        return new { request.Date };
    });

    app.MapDelete("/test/clock", (TestClock clock) => clock.Today = null);

    app.MapPost("/test/titles", (NewTitle request, Library library) =>
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        var title = library.AddTitle($"{request.Name} {tag}", "Test Author", request.Copies);
        return new { title.Id, title.Name };
    });

    app.MapPost("/test/members", (NewMember request, Library library) =>
    {
        var member = library.AddMember(request.Fines);
        for (var i = 1; i <= request.Loans; i++)
        {
            var filler = library.AddTitle($"Filler {i} for {member.Username}", "Test Author", 1);
            library.AddLoan(member.Username, filler.Id, Library.LoanDays);
        }
        return new { member.Username, member.Password, member.Name };
    });

    app.MapPost("/test/loans", (NewLoan request, Library library) =>
    {
        var loan = library.AddLoan(request.Username, request.TitleId, request.DueInDays);
        return new { loan.Id };
    });
}

app.Run();

record SetClock(DateOnly Date);
record NewTitle(string Name, int Copies = 1);
record NewMember(int Loans = 0, decimal Fines = 0);
record NewLoan(string Username, int TitleId, int DueInDays = Library.LoanDays);
