namespace TheCup_Application.Models;

public sealed class GroupSummary
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<TeamSummary?> Teams { get; init; }

    public string TeamCountDisplay => Teams.Count == 1 ? "1 team" : $"{Teams.Count} teams";
}
