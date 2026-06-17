using TheCup_Application.Models;
using TheCup_Domain.Enums;

namespace TheCup_Application.Services;

public static class ScheduleTypeCatalog
{
    public static IReadOnlyList<ScheduleTypeOption> CreateOptions() =>
    [
        new ScheduleTypeOption
        {
            Type = ScheduleType.Default,
            DisplayName = "Default"
        },
        new ScheduleTypeOption
        {
            Type = ScheduleType.RoundRobinPerPitch,
            DisplayName = "Round robin (group per pitch)"
        }
    ];

    public static bool IsAvailable(ScheduleType type, int groupCount, int pitchCount) => type switch
    {
        ScheduleType.Default => true,
        ScheduleType.RoundRobinPerPitch => groupCount > 0 && groupCount == pitchCount,
        _ => false
    };

    public static void EnsureAvailable(ScheduleType type, int groupCount, int pitchCount)
    {
        if (!IsAvailable(type, groupCount, pitchCount))
        {
            throw new InvalidOperationException(
                $"The selected schedule type is not available for {groupCount} groups and {pitchCount} pitches.");
        }
    }
}
