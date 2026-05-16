namespace TheCup_Domain.Services;

public static class GroupDrawPlanner
{
    public static IReadOnlyList<int> CalculateGroupSizes(
        int teamCount,
        int groupCount,
        int teamsPerGroup,
        int minTeamsPerGroup)
    {
        if (groupCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(groupCount), "Number of groups must be at least 1.");
        }

        if (minTeamsPerGroup < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minTeamsPerGroup), "Minimum teams per group must be at least 1.");
        }

        if (teamsPerGroup < minTeamsPerGroup)
        {
            throw new ArgumentException("Teams per group cannot be less than the minimum teams per group.");
        }

        if (teamCount < 1)
        {
            throw new InvalidOperationException("At least one team is required to generate groups.");
        }

        var requiredMinimum = groupCount * minTeamsPerGroup;
        if (teamCount < requiredMinimum)
        {
            throw new InvalidOperationException(
                $"At least {requiredMinimum} teams are required for {groupCount} groups with a minimum of {minTeamsPerGroup} teams each.");
        }

        var maximumCapacity = groupCount * teamsPerGroup;
        if (teamCount > maximumCapacity)
        {
            throw new InvalidOperationException(
                $"At most {maximumCapacity} teams fit into {groupCount} groups with up to {teamsPerGroup} teams each.");
        }

        var baseSize = teamCount / groupCount;
        var remainder = teamCount % groupCount;

        var sizes = new int[groupCount];
        for (var i = 0; i < groupCount; i++)
        {
            sizes[i] = baseSize + (i < remainder ? 1 : 0);
        }

        if (sizes.Min() < minTeamsPerGroup)
        {
            throw new InvalidOperationException(
                $"Teams cannot be divided into valid groups. Try fewer groups, a lower minimum, or more teams.");
        }

        if (sizes.Max() > teamsPerGroup)
        {
            throw new InvalidOperationException(
                $"Teams cannot be divided into valid groups. Try more groups or a higher teams-per-group limit.");
        }

        return sizes;
    }
}
