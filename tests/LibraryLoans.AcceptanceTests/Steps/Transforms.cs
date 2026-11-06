using System.Globalization;
using Reqnroll;

namespace LibraryLoans.AcceptanceTests.Steps;

[Binding]
public class Transforms
{
    [StepArgumentTransformation(@"(\d{1,2} \w+ \d{4})")]
    public DateOnly ToDate(string text) =>
        DateOnly.ParseExact(text, "d MMMM yyyy", CultureInfo.InvariantCulture);
}
