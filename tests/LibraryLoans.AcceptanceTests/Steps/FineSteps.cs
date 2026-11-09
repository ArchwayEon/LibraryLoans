using LibraryLoans.AcceptanceTests.Support;
using Reqnroll;

namespace LibraryLoans.AcceptanceTests.Steps;

[Binding]
public class FineSteps(LibraryWorld world, TestSupportClient support)
{
    [Given("{word} owes ${decimal} in fines")]
    public Task Owes(string member, decimal fines) =>
        support.SetFinesAsync(world.Members[member], fines);
}
