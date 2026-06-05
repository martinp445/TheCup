using TheCup_Domain.Enums;

namespace TheCup_Domain.Entities
{
    public class Game
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public string Name { get; init; } = "Game";

        public Guid PitchId { get; set; }

        public ValueTuple<Guid, Guid> Teams { get; set; }

        public GameStatus Status { get; set; } = GameStatus.Scheduled;

        public int HomeTeamScore { get; set; } = 0;

        public int AwayTeamScore { get; set; } = 0;
    }
}
