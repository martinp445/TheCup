
namespace TheCup_Domain.Entities
{
    public class Game
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public Guid PitchId { get; set; }

        public ValueTuple<Guid, Guid> Teams { get; set; }
    }
}
