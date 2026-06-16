using TheCup_Domain.Entities;

namespace TheCup_Domain.Services
{
    public class GameScheduler
    {
        public static IEnumerable<Game> GenerateSchedule(Tournament tournament)
        {
            List<Game> schedule = new List<Game>();

            if (tournament.Groups.Count == tournament.Pitches.Count)
            {
                var groupMatches = new List<List<(Guid teamA, Guid teamB)>>();
                foreach (var group in tournament.Groups)
                {
                    groupMatches.Add(MakeGroupMatches(group).OrderBy(_ => Random.Shared.Next()).ToList());
                }

                var scheduledMatches = PitchPerGroupScheduler.ScheduleMatches(groupMatches, tournament.Pitches.Select(p => p.Id));

                schedule = scheduledMatches.Select((match, index) => new Game
                {
                    Id = Guid.NewGuid(),
                    Name = $"Game {index + 1}",
                    PitchId = match.Pitch,
                    Teams = (match.TeamA, match.TeamB)
                }).ToList();
            }
            else
            {
                var allMatches = new List<ValueTuple<Guid, Guid>>();
                foreach (var group in tournament.Groups)
                {
                    allMatches.AddRange(MakeGroupMatches(group));
                }

                var scheduledMatches = DefaultGameScheduler.ScheduleMatches(allMatches, tournament.Pitches.Select(p => p.Id));

                schedule = scheduledMatches.Select((match, index) => new Game
                {
                    Id = Guid.NewGuid(),
                    Name = $"Game {index + 1}",
                    PitchId = match.Pitch,
                    Teams = (match.TeamA, match.TeamB)
                }).ToList();
            }

            return schedule;
        }

        public static IEnumerable<ValueTuple<Guid, Guid>> MakeGroupMatches(Group group)
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