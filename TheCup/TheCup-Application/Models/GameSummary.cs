namespace TheCup_Application.Models
{
    public sealed class GameSummary
    {
        public required Guid Id { get; init; }

        public required string Name { get; init; }

        public required ValueTuple<string, string> TeamNames { get; init; }
    }
}
