using TheCup_Domain.Enums;

namespace TheCup_Application.Models;

public sealed class TournamentSummary
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required DateOnly StartDate { get; init; }

    public Sport Sport { get; init; }

    public TournamentStatus Status { get; init; }

    public string FormattedStartDate => StartDate.ToString("d MMMM yyyy");

    public string SportDisplayName => Sport.ToString();

    public string StatusDisplayName => Status.ToString();

    public int PitchCount { get; init; }

    public int TeamCount { get; init; }

    public bool HasGroups { get; init; }

    public bool TeamsConfirmed { get; init; }

    public string PitchCountDisplay => PitchCount == 1 ? "1 pitch" : $"{PitchCount} pitches";

    public string TeamCountDisplay => TeamCount == 1 ? "1 team" : $"{TeamCount} teams";

    public bool IsDraft => Status == TournamentStatus.Draft;

    public bool IsActive => Status == TournamentStatus.Active;

    public bool CanActivate =>
        IsDraft && TeamsConfirmed && TeamCount >= 1 && PitchCount >= 1;
}
