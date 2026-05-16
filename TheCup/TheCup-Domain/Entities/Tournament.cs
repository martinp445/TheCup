using TheCup_Domain.Enums;
using TheCup_Domain.ValueObjects;

namespace TheCup_Domain.Entities;

public class Tournament
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; init; }

    public DateOnly StartDate { get; init; } = DateOnly.FromDateTime(DateTime.Today);

    public Sport Sport { get; init; } = Sport.Football;

    public TournamentStatus Status { get; set; } = TournamentStatus.Draft;

    public bool TeamsConfirmed { get; set; }

    public List<Team> Teams { get; } = [];

    public List<Pitch> Pitches { get; } = [];

    public List<Group> Groups { get; } = [];

    public GroupStageSettings? GroupStageSettings { get; set; }
}
