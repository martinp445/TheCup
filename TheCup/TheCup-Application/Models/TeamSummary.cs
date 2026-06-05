namespace TheCup_Application.Models;

public sealed class TeamSummary
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public int Points { get; set; }

    public int Goals { get; set; }

    public int ConcededGoals { get; set; }
}
