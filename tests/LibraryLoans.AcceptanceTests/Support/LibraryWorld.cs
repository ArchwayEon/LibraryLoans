namespace LibraryLoans.AcceptanceTests.Support;

/// <summary>Per-scenario state shared by step definitions through context injection.</summary>
public class LibraryWorld
{
    /// <summary>The date the scenario set with "today is".</summary>
    public DateOnly Today { get; set; }

    /// <summary>Members by the name used in the feature file, such as "Mia".</summary>
    public Dictionary<string, TestMember> Members { get; } = new();

    /// <summary>Titles by the name used in the feature file, such as "Dune".</summary>
    public Dictionary<string, TestTitle> Titles { get; } = new();

    /// <summary>Loans by the title name used in the feature file.</summary>
    public Dictionary<string, TestLoan> Loans { get; } = new();

    public string? LastMessage { get; set; }

    /// <summary>
    /// Replaces each feature-file title name with the unique name the app stored,
    /// so that "Renewed Dune." matches "Renewed Dune 3fa2c1.".
    /// </summary>
    public string ToActualText(string text)
    {
        foreach (var (name, title) in Titles)
            text = text.Replace(name, title.Name);
        return text;
    }
}
