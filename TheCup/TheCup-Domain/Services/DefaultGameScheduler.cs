namespace TheCup_Domain.Services
{
    internal class DefaultGameScheduler
    {
        public static IEnumerable<Match> ScheduleMatches(List<(Guid TeamA, Guid TeamB)> matches, IEnumerable<Guid> pitches)
        {
            var remaining = matches.ToList();
            var pitchList = pitches.ToList();
            var scheduledMatches = new List<Match>();

            if (pitchList.Count == 0)
                throw new ArgumentException("At least one pitch is required.", nameof(pitches));

            // Stores the last slot index where a team played.
            var lastPlayedSlot = new Dictionary<Guid, int>();

            // Stores how many matches each team has played.
            var matchesPlayed = new Dictionary<Guid, int>();

            // Stores how many times each team has played on a specific pitch.
            var pitchUsageByTeam = new Dictionary<Guid, Dictionary<Guid, int>>();

            // Stores total usage count for each pitch.
            var pitchLoad = pitchList.ToDictionary(p => p, _ => 0);

            // Stores all teams that played in the immediately previous slot.
            var previousSlotTeams = new HashSet<Guid>();

            int currentSlot = 0;

            while (remaining.Count > 0)
            {
                var bestSlotMatches = BuildBestSlot(
                    remaining,
                    pitchList.Count,
                    currentSlot,
                    previousSlotTeams,
                    lastPlayedSlot,
                    matchesPlayed);

                // Safety guard to avoid an infinite loop if no slot can be built.
                if (bestSlotMatches.Count == 0)
                {
                    bestSlotMatches.Add(remaining[0]);
                }

                // Assign pitches fairly after the slot has been selected.
                var availablePitches = new HashSet<Guid>(pitchList);

                foreach (var match in bestSlotMatches
                             .OrderByDescending(m => PitchAssignmentPriority(
                                 m,
                                 pitchUsageByTeam,
                                 matchesPlayed)))
                {
                    var bestPitch = availablePitches
                        .OrderBy(p => ScorePitch(
                            match,
                            p,
                            pitchUsageByTeam,
                            pitchLoad))
                        .First();

                    scheduledMatches.Add(new Match
                    {
                        TeamA = match.TeamA,
                        TeamB = match.TeamB,
                        Pitch = bestPitch,
                        Round = currentSlot + 1
                    });

                    availablePitches.Remove(bestPitch);

                    UpdateAfterSchedulingMatch(
                        match,
                        bestPitch,
                        currentSlot,
                        lastPlayedSlot,
                        matchesPlayed,
                        pitchUsageByTeam,
                        pitchLoad);
                }

                // Remove selected matches from remaining.
                foreach (var match in bestSlotMatches)
                {
                    remaining.Remove(match);
                }

                // Track teams used in this slot for the next round's fairness rules.
                previousSlotTeams = bestSlotMatches
                    .SelectMany(m => new[] { m.TeamA, m.TeamB })
                    .ToHashSet();

                currentSlot++;
            }

            return scheduledMatches;
        }

        private static List<(Guid TeamA, Guid TeamB)> BuildBestSlot(
            List<(Guid TeamA, Guid TeamB)> remaining,
            int maxMatchesInSlot,
            int currentSlot,
            HashSet<Guid> previousSlotTeams,
            Dictionary<Guid, int> lastPlayedSlot,
            Dictionary<Guid, int> matchesPlayed)
        {
            var best = new SlotCandidate();

            SearchBestSlot(
                remaining,
                startIndex: 0,
                maxMatchesInSlot: maxMatchesInSlot,
                currentSlot: currentSlot,
                previousSlotTeams: previousSlotTeams,
                lastPlayedSlot: lastPlayedSlot,
                matchesPlayed: matchesPlayed,
                usedTeams: new HashSet<Guid>(),
                current: new List<(Guid TeamA, Guid TeamB)>(),
                best: best);

            return best.Matches;
        }

        private static void SearchBestSlot(
            List<(Guid TeamA, Guid TeamB)> remaining,
            int startIndex,
            int maxMatchesInSlot,
            int currentSlot,
            HashSet<Guid> previousSlotTeams,
            Dictionary<Guid, int> lastPlayedSlot,
            Dictionary<Guid, int> matchesPlayed,
            HashSet<Guid> usedTeams,
            List<(Guid TeamA, Guid TeamB)> current,
            SlotCandidate best)
        {
            int currentScore = ScoreSlot(
                current,
                currentSlot,
                previousSlotTeams,
                lastPlayedSlot,
                matchesPlayed);

            if (IsBetterSlot(current, currentScore, best))
            {
                best.Matches = current.ToList();
                best.Score = currentScore;
            }

            if (current.Count == maxMatchesInSlot)
                return;

            for (int i = startIndex; i < remaining.Count; i++)
            {
                var match = remaining[i];

                // Skip matches that would create a team conflict inside the same slot.
                if (usedTeams.Contains(match.TeamA) || usedTeams.Contains(match.TeamB))
                    continue;

                usedTeams.Add(match.TeamA);
                usedTeams.Add(match.TeamB);
                current.Add(match);

                SearchBestSlot(
                    remaining,
                    i + 1,
                    maxMatchesInSlot,
                    currentSlot,
                    previousSlotTeams,
                    lastPlayedSlot,
                    matchesPlayed,
                    usedTeams,
                    current,
                    best);

                current.RemoveAt(current.Count - 1);
                usedTeams.Remove(match.TeamA);
                usedTeams.Remove(match.TeamB);
            }
        }

        private static bool IsBetterSlot(
            List<(Guid TeamA, Guid TeamB)> current,
            int currentScore,
            SlotCandidate best)
        {
            // Always prefer filling more pitches first.
            if (current.Count != best.Matches.Count)
                return current.Count > best.Matches.Count;

            return currentScore > best.Score;
        }

        private static int ScoreSlot(
            List<(Guid TeamA, Guid TeamB)> slotMatches,
            int currentSlot,
            HashSet<Guid> previousSlotTeams,
            Dictionary<Guid, int> lastPlayedSlot,
            Dictionary<Guid, int> matchesPlayed)
        {
            int score = 0;

            foreach (var match in slotMatches)
            {
                score += ScoreMatch(
                    match,
                    currentSlot,
                    previousSlotTeams,
                    lastPlayedSlot,
                    matchesPlayed);
            }

            return score;
        }

        private static int ScoreMatch(
            (Guid TeamA, Guid TeamB) match,
            int currentSlot,
            HashSet<Guid> previousSlotTeams,
            Dictionary<Guid, int> lastPlayedSlot,
            Dictionary<Guid, int> matchesPlayed)
        {
            int teamALast = GetLastPlayedSlot(match.TeamA, lastPlayedSlot);
            int teamBLast = GetLastPlayedSlot(match.TeamB, lastPlayedSlot);

            int teamARest = currentSlot - teamALast;
            int teamBRest = currentSlot - teamBLast;

            int teamAPlayed = GetCount(matchesPlayed, match.TeamA);
            int teamBPlayed = GetCount(matchesPlayed, match.TeamB);

            int score = 0;

            // Prefer teams that had a longer rest.
            score += Math.Min(teamARest, teamBRest) * 100;
            score += (teamARest + teamBRest) * 20;

            // Strongly penalize teams that played in the previous slot.
            if (previousSlotTeams.Contains(match.TeamA))
                score -= 800;

            if (previousSlotTeams.Contains(match.TeamB))
                score -= 800;

            // Slightly prefer teams with fewer already played matches.
            score -= (teamAPlayed + teamBPlayed) * 10;

            // Slightly prefer balanced progression between both teams.
            score -= Math.Abs(teamAPlayed - teamBPlayed) * 5;

            return score;
        }

        private static int PitchAssignmentPriority(
            (Guid TeamA, Guid TeamB) match,
            Dictionary<Guid, Dictionary<Guid, int>> pitchUsageByTeam,
            Dictionary<Guid, int> matchesPlayed)
        {
            int usageSpread =
                GetPitchUsageVariance(match.TeamA, pitchUsageByTeam) +
                GetPitchUsageVariance(match.TeamB, pitchUsageByTeam);

            int played =
                GetCount(matchesPlayed, match.TeamA) +
                GetCount(matchesPlayed, match.TeamB);

            return usageSpread * 10 + played;
        }

        private static int ScorePitch(
            (Guid TeamA, Guid TeamB) match,
            Guid pitch,
            Dictionary<Guid, Dictionary<Guid, int>> pitchUsageByTeam,
            Dictionary<Guid, int> pitchLoad)
        {
            int teamAPitchUsage = GetNestedCount(pitchUsageByTeam, match.TeamA, pitch);
            int teamBPitchUsage = GetNestedCount(pitchUsageByTeam, match.TeamB, pitch);
            int totalPitchLoad = GetCount(pitchLoad, pitch);

            int score = 0;

            // Lower score means a better pitch.
            // Avoid sending the same teams to the same pitch repeatedly.
            score += (teamAPitchUsage + teamBPitchUsage) * 100;

            // Slightly balance overall pitch usage.
            score += totalPitchLoad * 5;

            // Slightly penalize uneven pitch history between both teams.
            score += Math.Abs(teamAPitchUsage - teamBPitchUsage) * 10;

            return score;
        }

        private static void UpdateAfterSchedulingMatch(
            (Guid TeamA, Guid TeamB) match,
            Guid pitch,
            int currentSlot,
            Dictionary<Guid, int> lastPlayedSlot,
            Dictionary<Guid, int> matchesPlayed,
            Dictionary<Guid, Dictionary<Guid, int>> pitchUsageByTeam,
            Dictionary<Guid, int> pitchLoad)
        {
            lastPlayedSlot[match.TeamA] = currentSlot;
            lastPlayedSlot[match.TeamB] = currentSlot;

            Increment(matchesPlayed, match.TeamA);
            Increment(matchesPlayed, match.TeamB);

            IncrementNested(pitchUsageByTeam, match.TeamA, pitch);
            IncrementNested(pitchUsageByTeam, match.TeamB, pitch);

            Increment(pitchLoad, pitch);
        }

        private static int GetLastPlayedSlot(Guid teamId, Dictionary<Guid, int> lastPlayedSlot)
        {
            // Teams that never played get a very low slot value,
            // which gives them a large rest advantage.
            return lastPlayedSlot.TryGetValue(teamId, out var slot) ? slot : -1000;
        }

        private static int GetCount<TKey>(Dictionary<TKey, int> dictionary, TKey key)
            where TKey : notnull
        {
            return dictionary.TryGetValue(key, out var value) ? value : 0;
        }

        private static int GetNestedCount(
            Dictionary<Guid, Dictionary<Guid, int>> dictionary,
            Guid outerKey,
            Guid innerKey)
        {
            if (!dictionary.TryGetValue(outerKey, out var inner))
                return 0;

            return inner.TryGetValue(innerKey, out var value) ? value : 0;
        }

        private static void Increment<TKey>(Dictionary<TKey, int> dictionary, TKey key)
            where TKey : notnull
        {
            dictionary[key] = GetCount(dictionary, key) + 1;
        }

        private static void IncrementNested(
            Dictionary<Guid, Dictionary<Guid, int>> dictionary,
            Guid outerKey,
            Guid innerKey)
        {
            if (!dictionary.TryGetValue(outerKey, out var inner))
            {
                inner = new Dictionary<Guid, int>();
                dictionary[outerKey] = inner;
            }

            inner[innerKey] = inner.TryGetValue(innerKey, out var value) ? value + 1 : 1;
        }

        private static int GetPitchUsageVariance(
            Guid teamId,
            Dictionary<Guid, Dictionary<Guid, int>> pitchUsageByTeam)
        {
            if (!pitchUsageByTeam.TryGetValue(teamId, out var usage) || usage.Count == 0)
                return 0;

            int min = usage.Values.Min();
            int max = usage.Values.Max();

            return max - min;
        }

        private sealed class SlotCandidate
        {
            public List<(Guid TeamA, Guid TeamB)> Matches { get; set; } = new();
            public int Score { get; set; } = int.MinValue;
        }
    }
}
