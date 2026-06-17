using TheCup_Domain.Entities;

namespace TheCup_Domain.Services
{
    internal class PitchPerGroupScheduler
    {
        public static IEnumerable<Match> ScheduleMatches(List<List<(Guid teamA, Guid teamB)>> groupMatches, IEnumerable<Guid> pitches)
        {
            var result = new List<Match>();
            var pitchList = pitches.ToList();

            for (int g = 0; g < groupMatches.Count; g++)
            {
                var matches = groupMatches[g];
                var pitch = pitchList[g];

                // rounds for this group
                var rounds = new List<List<(Guid A, Guid B)>>();

                // track last round played per team
                var lastRoundPlayed = new Dictionary<Guid, int>();

                foreach (var m in matches)
                {
                    bool placed = false;

                    for (int r = 0; r < rounds.Count; r++)
                    {
                        var round = rounds[r];

                        bool teamBusyInRound = round.Any(x =>
                            x.A == m.teamA || x.B == m.teamA ||
                            x.A == m.teamB || x.B == m.teamB);

                        bool violatesRest =
                            (lastRoundPlayed.TryGetValue(m.teamA, out int lastA) && lastA == r - 1) ||
                            (lastRoundPlayed.TryGetValue(m.teamB, out int lastB) && lastB == r - 1);

                        if (!teamBusyInRound && !violatesRest)
                        {
                            round.Add(m);
                            lastRoundPlayed[m.teamA] = r;
                            lastRoundPlayed[m.teamB] = r;
                            placed = true;
                            break;
                        }
                    }

                    if (!placed)
                    {
                        int newRoundIndex = rounds.Count;

                        rounds.Add(new List<(Guid A, Guid B)> { (m.teamA, m.teamB) });

                        lastRoundPlayed[m.teamA] = newRoundIndex;
                        lastRoundPlayed[m.teamB] = newRoundIndex;
                    }
                }

                int matchNumberInGroup = 1;

                // flatten into result with pitch assigned
                foreach (var round in rounds)
                {
                    foreach (var m in round)
                    {
                        result.Add(new Match
                        {
                            TeamA = m.A,
                            TeamB = m.B,
                            Pitch = pitch,
                            Round = matchNumberInGroup++
                        });
                    }
                }
            }

            return result.OrderBy(m => m.Round);
        }
    }
}
