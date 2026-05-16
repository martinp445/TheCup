namespace TheCup_Application.Models;

public sealed class GroupSummary
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<string> TeamNames { get; init; }

    public string TeamCountDisplay => TeamNames.Count == 1 ? "1 team" : $"{TeamNames.Count} teams";
}
