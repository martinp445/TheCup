using TheCup_Application.Models;

namespace TheCup_Application.Services;

public static class TournamentActivationRules
{
    public static bool CanActivate(TournamentSummary summary) =>
        summary.IsDraft
        && summary.TeamsConfirmed
        && summary.TeamCount >= 1
        && summary.PitchCount >= 1;

    public static string GetActivationHint(TournamentSummary summary)
    {
        if (summary.IsActive)
        {
            return "Tournament is active. Open Groups to run the group draw.";
        }

        if (!summary.TeamsConfirmed)
        {
            return "Confirm teams on the Teams page to enable activation.";
        }

        if (summary.TeamCount < 1)
        {
            return "Add at least one team before activating.";
        }

        if (summary.PitchCount < 1)
        {
            return "Add at least one pitch before activating.";
        }

        return "All requirements met. You can activate the tournament now.";
    }
}
