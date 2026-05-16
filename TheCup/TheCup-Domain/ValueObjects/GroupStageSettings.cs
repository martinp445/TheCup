namespace TheCup_Domain.ValueObjects;

public sealed class GroupStageSettings
{
    public required int GroupCount { get; init; }

    public required int TeamsPerGroup { get; init; }

    public required int MinTeamsPerGroup { get; init; }
}
