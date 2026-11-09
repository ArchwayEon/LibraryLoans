using LibraryLoans.AcceptanceTests.Support;
using Reqnroll;

namespace LibraryLoans.AcceptanceTests.Steps;

[Binding]
public class HoldSteps(LibraryWorld world, TestSupportClient support)
{
    [Given("{word} has placed a hold on {string}")]
    public async Task HasPlacedAHold(string member, string title)
    {
        // A member who only appears as "the other member" is created on first mention.
        if (!world.Members.TryGetValue(member, out var holder))
            world.Members[member] = holder = await support.CreateMemberAsync();

        await support.PlaceHoldAsync(holder, world.Titles[title]);
    }
}
