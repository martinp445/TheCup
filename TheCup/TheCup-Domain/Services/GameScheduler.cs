using TheCup_Domain.Entities;

namespace TheCup_Domain.Services
{
    public class GameScheduler
    {
        public static IEnumerable<Game> GenerateSchedule(Tournament tournament)
        {
            var allMatches = new List<ValueTuple<Guid, Guid>>();
            foreach (var group in tournament.Groups)
            {
                allMatches.AddRange(MakeGroupMatches(group));
            }

            return allMatches.Select((match, index) => new Game
            {
                Id = Guid.NewGuid(),
                Name = $"Game {index + 1}",
                PitchId = tournament.Pitches[index % tournament.Pitches.Count].Id,
                Teams = match
            });
        }

        private static IEnumerable<ValueTuple<Guid, Guid>> MakeGroupMatches(Group group)
        {
            var result = new List<ValueTuple<Guid, Guid>>();
            var size = group.TeamIds.Count;

            for (int i = 0; i < size; i++)
            {
                for (int j = i + 1; j < size; j++)
                {
                    result.Add((group.TeamIds[i], group.TeamIds[j]));
                }
            }

            return result;
        }
    }
}
