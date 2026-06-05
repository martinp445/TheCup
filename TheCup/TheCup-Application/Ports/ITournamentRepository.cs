using TheCup_Application.Models;
using TheCup_Domain.Enums;

namespace TheCup_Application.Ports;

public interface ITournamentRepository
{
    Task<IReadOnlyList<TournamentSummary>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TournamentSummary?> GetByIdAsync(Guid tournamentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamSummary>> GetTeamsAsync(Guid tournamentId, CancellationToken cancellationToken = default);

    Task<TournamentSummary> CreateAsync(string name, Sport sport, CancellationToken cancellationToken = default);

    Task<TeamSummary> AddTeamAsync(Guid tournamentId, string name, CancellationToken cancellationToken = default);

    Task RenameTeamAsync(Guid tournamentId, Guid teamId, string newName, CancellationToken cancellationToken = default);

    Task RemoveTeamAsync(Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default);

    Task<TournamentSummary> ConfirmTeamsAsync(Guid tournamentId, CancellationToken cancellationToken = default);

    Task<TournamentSummary> UnconfirmTeamsAsync(Guid tournamentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PitchSummary>> GetPitchesAsync(Guid tournamentId, CancellationToken cancellationToken = default);

    Task<PitchSummary> AddPitchAsync(Guid tournamentId, string name, CancellationToken cancellationToken = default);

    Task RenamePitchAsync(Guid tournamentId, Guid pitchId, string newName, CancellationToken cancellationToken = default);

    Task RemovePitchAsync(Guid tournamentId, Guid pitchId, CancellationToken cancellationToken = default);

    Task<TournamentSummary> StartTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupSummary>> GetGroupsAsync(Guid tournamentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupSummary>> GenerateGroupsAsync(
        Guid tournamentId,
        int groupCount,
        int teamsPerGroup,
        int minTeamsPerGroup,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameSummary>> GetScheduleAsync(Guid tournamentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameSummary>> GenerateScheduleAsync(Guid tournamentId, CancellationToken cancellationToken = default);

    Task<GameStatus> StartGameAsync(Guid tournamentId, Guid gameId, CancellationToken cancellationToken = default);

    Task<GameStatus> FinishGameAsync(Guid tournamentId, Guid gameId, int homeTeamScore, int awayTeamScore, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid tournamentId, CancellationToken cancellationToken = default);

    Task SaveAsync(Guid tournamentId, CancellationToken cancellationToken = default);
}
