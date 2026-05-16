namespace TheCup_Application.Services;

internal static class TournamentValidation
{
    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        return name.Trim();
    }

    public static void EnsureUniqueTeamName(IEnumerable<string> existingNames, string candidateName)
    {
        EnsureUniqueName(existingNames, candidateName, "team");
    }

    public static void EnsureUniquePitchName(IEnumerable<string> existingNames, string candidateName)
    {
        EnsureUniqueName(existingNames, candidateName, "pitch");
    }

    public static void EnsureMinimumPitchCount(int pitchCount)
    {
        if (pitchCount < 1)
        {
            throw new InvalidOperationException("A tournament must have at least one pitch.");
        }
    }

    private static void EnsureUniqueName(IEnumerable<string> existingNames, string candidateName, string entityLabel)
    {
        var normalized = NormalizeName(candidateName);
        var duplicate = existingNames.Any(n =>
            string.Equals(n, normalized, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"A {entityLabel} named \"{normalized}\" already exists in this tournament.");
        }
    }
}
