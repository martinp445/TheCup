using TheCup_Domain.Enums;

namespace TheCup_Application.Models;

public sealed class SportOption
{
    public required Sport Sport { get; init; }

    public required string DisplayName { get; init; }
}
