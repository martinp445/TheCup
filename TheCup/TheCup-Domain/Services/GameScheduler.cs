using TheCup_Domain.Entities;

namespace TheCup_Domain.Services
{
    public class GameScheduler
    {
        private struct Match
        {
            public Guid TeamA { get; set; }
            public Guid TeamB { get; set; }
            public Guid Pitch { get; set; }
        }

        public static IEnumerable<Game> GenerateSchedule(Tournament tournament)
        {
            var allMatches = new List<ValueTuple<Guid, Guid>>();
            foreach (var group in tournament.Groups)
            {
                allMatches.AddRange(MakeGroupMatches(group));
            }

            var scheduledMatches = ScheduleMatches(allMatches, tournament.Pitches.Select(p => p.Id));

            return scheduledMatches.Select((match, index) => new Game
            {
                Id = Guid.NewGuid(),
                Name = $"Game {index + 1}",
                PitchId = match.Pitch,
                Teams = (match.TeamA, match.TeamB)
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

        private static IEnumerable<Match> ScheduleMatches(List<ValueTuple<Guid, Guid>> matches, IEnumerable<Guid> pitches)
        {
            var remaining = matches.ToList();
            var pitchList = pitches.ToList();
            var scheduledMatches = new List<Match>();

            (Guid TeamA, Guid TeamB)? lastScheduled = null;

            int pitchIndex = 0;
            while (remaining.Count > 0)
            {
                Guid pitchId = pitchList[pitchIndex % pitchList.Count];

                // First try: find match where no team played in previous match
                int matchIndex = remaining.FindIndex(m =>
                    !lastScheduled.HasValue || !SharesTeam(m, lastScheduled.Value));

                // Fallback: if impossible, take first remaining
                if (matchIndex == -1)
                    matchIndex = 0;

                var match = remaining[matchIndex];
                remaining.RemoveAt(matchIndex);

                scheduledMatches.Add(new Match
                {
                    TeamA = match.Item1,
                    TeamB = match.Item2,
                    Pitch = pitchId
                });

                lastScheduled = (match.Item1, match.Item2);
                pitchIndex++;
            }


            return scheduledMatches;
        }

        private static bool SharesTeam((Guid TeamA, Guid TeamB) match1, (Guid TeamA, Guid TeamB) match2)
        {
            return match1.TeamA == match2.TeamA || match1.TeamA == match2.TeamB ||
                   match1.TeamB == match2.TeamA || match1.TeamB == match2.TeamB;
        }

    }
}