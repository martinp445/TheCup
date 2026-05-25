namespace TheCup_Application.Models
{
    public sealed class GameSummary
    {
        public required Guid Id { get; init; }

        public required string Name { get; init; }

        public required string PitchName { get; init; }

        public required string HomeTeamName { get; init; }

        public required string AwayTeamName { get; init; }
    }
}
