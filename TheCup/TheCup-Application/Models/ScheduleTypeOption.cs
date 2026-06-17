using TheCup_Domain.Enums;

namespace TheCup_Application.Models;

public sealed class ScheduleTypeOption
{
    public required ScheduleType Type { get; init; }

    public required string DisplayName { get; init; }

    public bool IsAvailable { get; set; } = true;
}
