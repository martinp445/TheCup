namespace TheCup_Infrastructure.Services;

public sealed class GameSchedulePdfRequest
{
    public required string TournamentName { get; init; }

    public required IReadOnlyList<GameSchedulePdfRow> Games { get; init; }
}
