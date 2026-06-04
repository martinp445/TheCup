using TheCup_Domain.Entities;

namespace TheCup_Infrastructure.Services;

public interface ITournamentPersistenceService
{
    /// <summary>
    /// Saves a tournament to persistent storage (JSON file).
    /// </summary>
    /// <param name="tournament">The tournament entity to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveTournamentAsync(Tournament tournament, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all tournaments from persistent storage (JSON files).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of loaded tournaments, or empty list if none exist.</returns>
    Task<IReadOnlyList<Tournament>> LoadTournamentsAsync(CancellationToken cancellationToken = default);

    Task DeleteTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);
}
