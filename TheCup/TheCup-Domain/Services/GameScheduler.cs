using TheCup_Domain.Entities;
using TheCup_Domain.Enums;

namespace TheCup_Domain.Services;

public class GameScheduler
{
    public static IEnumerable<Game> GenerateSchedule(Tournament tournament, ScheduleType scheduleType)
    {
        return scheduleType switch
        {
            ScheduleType.Default => GenerateDefaultSchedule(tournament),
            ScheduleType.RoundRobinPerPitch => GenerateRoundRobinPerPitchSchedule(tournament),
            _ => throw new ArgumentOutOfRangeException(nameof(scheduleType), scheduleType, "Unsupported schedule type.")
        };
    }

    private static IEnumerable<Game> GenerateDefaultSchedule(Tournament tournament)
    {
        var allMatches = new List<(Guid TeamA, Guid TeamB)>();
        foreach (var group in tournament.Groups)
        {
            allMatches.AddRange(MakeGroupMatches(group));
        }

        var scheduledMatches = DefaultGameScheduler.ScheduleMatches(allMatches, tournament.Pitches.Select(p => p.Id));

        return scheduledMatches.Select((match, index) => new Game
        {
            Id = Guid.NewGuid(),
            Name = $"Game {index + 1}",
            PitchId = match.Pitch,
            Teams = (match.TeamA, match.TeamB)
        });
    }

    private static IEnumerable<Game> GenerateRoundRobinPerPitchSchedule(Tournament tournament)
    {
        if (tournament.Groups.Count != tournament.Pitches.Count)
        {
            throw new InvalidOperationException(
                "Round robin (group per pitch) requires the number of groups to equal the number of pitches.");
        }

        var groupMatches = new List<List<(Guid teamA, Guid teamB)>>();
        foreach (var group in tournament.Groups)
        {
            groupMatches.Add(MakeGroupMatches(group).OrderBy(_ => Random.Shared.Next()).ToList());
        }

        var scheduledMatches = PitchPerGroupScheduler.ScheduleMatches(
            groupMatches,
            tournament.Pitches.Select(p => p.Id));

        return scheduledMatches.Select((match, index) => new Game
        {
            Id = Guid.NewGuid(),
            Name = $"Game {index + 1}",
            PitchId = match.Pitch,
            Teams = (match.TeamA, match.TeamB)
        });
    }

    public static IEnumerable<(Guid TeamA, Guid TeamB)> MakeGroupMatches(Group group)
    {
        var result = new List<(Guid TeamA, Guid TeamB)>();
        var size = group.TeamIds.Count;

        for (var i = 0; i < size; i++)
        {
            for (var j = i + 1; j < size; j++)
            {
                result.Add((group.TeamIds[i], group.TeamIds[j]));
            }
        }

        return result;
    }
}
