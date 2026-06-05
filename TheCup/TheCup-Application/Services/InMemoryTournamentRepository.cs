using System.IO;
using System.Text.Json;
using TheCup_Application.Models;
using TheCup_Application.Ports;
using TheCup_Domain.Entities;
using TheCup_Domain.Enums;
using TheCup_Domain.Services;
using TheCup_Domain.ValueObjects;
using TheCup_Infrastructure.Enviroment;
using TheCup_Infrastructure.Services;

namespace TheCup_Application.Services;

/// <summary>
/// Temporary in-memory store until JSON persistence is implemented.
/// </summary>
public sealed class InMemoryTournamentRepository : ITournamentRepository
{
    private readonly List<Tournament> _tournaments = [];
    private readonly ITournamentPersistenceService _persistenceService;

    public InMemoryTournamentRepository(ITournamentPersistenceService persistenceService)
    {
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
    }

    /// <summary>
    /// Initializes the repository by loading persisted tournaments from JSON files.
    /// Call this after construction to populate the in-memory store.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var loadedTournaments = await _persistenceService.LoadTournamentsAsync(cancellationToken).ConfigureAwait(false);
        _tournaments.AddRange(loadedTournaments);
    }

    public Task<IReadOnlyList<TournamentSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<TournamentSummary> results = _tournaments
            .Select(t =>
            {
                EnsureMinimumPitches(t);
                return ToSummary(t);
            })
            .OrderByDescending(t => t.StartDate)
            .ToList();

        return Task.FromResult(results);
    }

    public Task<TournamentSummary?> GetByIdAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId);
        if (tournament is null)
        {
            return Task.FromResult<TournamentSummary?>(null);
        }

        EnsureMinimumPitches(tournament);
        return Task.FromResult<TournamentSummary?>(ToSummary(tournament));
    }

    public Task<IReadOnlyList<TeamSummary>> GetTeamsAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        IReadOnlyList<TeamSummary> teams = tournament.Teams
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToTeamSummary)
            .ToList();

        return Task.FromResult(teams);
    }

    public Task<TournamentSummary> CreateAsync(string name, Sport sport, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedName = TournamentValidation.NormalizeName(name);

        var tournament = new Tournament
        {
            Name = normalizedName,
            Sport = sport
        };

        tournament.Pitches.Add(new Pitch { Name = "Pitch 1" });

        _tournaments.Add(tournament);
        return Task.FromResult(ToSummary(tournament));
    }

    public Task<TeamSummary> AddTeamAsync(Guid tournamentId, string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        var normalizedName = TournamentValidation.NormalizeName(name);
        TournamentValidation.EnsureUniqueTeamName(
            tournament.Teams.Select(t => t.Name),
            normalizedName);

        tournament.TeamsConfirmed = false;

        var team = new Team { Name = normalizedName };
        tournament.Teams.Add(team);

        return Task.FromResult(ToTeamSummary(team));
    }

    public Task RenameTeamAsync(Guid tournamentId, Guid teamId, string newName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        var team = tournament.Teams.FirstOrDefault(t => t.Id == teamId)
            ?? throw new InvalidOperationException("Team was not found.");

        var normalizedName = TournamentValidation.NormalizeName(newName);
        TournamentValidation.EnsureUniqueTeamName(
            tournament.Teams.Where(t => t.Id != teamId).Select(t => t.Name),
            normalizedName);

        team.Name = normalizedName;
        tournament.TeamsConfirmed = false;

        return Task.CompletedTask;
    }

    public Task RemoveTeamAsync(Guid tournamentId, Guid teamId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        var removed = tournament.Teams.RemoveAll(t => t.Id == teamId);
        if (removed == 0)
        {
            throw new InvalidOperationException("Team was not found.");
        }

        tournament.TeamsConfirmed = false;

        return Task.CompletedTask;
    }

    public Task<TournamentSummary> ConfirmTeamsAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        if (tournament.Teams.Count < 1)
        {
            throw new InvalidOperationException("Add at least one team before confirming.");
        }

        tournament.TeamsConfirmed = true;
        return Task.FromResult(ToSummary(tournament));
    }

    public Task<TournamentSummary> UnconfirmTeamsAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        tournament.TeamsConfirmed = false;
        tournament.Groups.Clear();
        tournament.GroupStageSettings = null;

        return Task.FromResult(ToSummary(tournament));
    }

    public Task<IReadOnlyList<PitchSummary>> GetPitchesAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        EnsureMinimumPitches(tournament);

        IReadOnlyList<PitchSummary> pitches = tournament.Pitches
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToPitchSummary)
            .ToList();

        return Task.FromResult(pitches);
    }

    public Task<PitchSummary> AddPitchAsync(Guid tournamentId, string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        var normalizedName = TournamentValidation.NormalizeName(name);
        TournamentValidation.EnsureUniquePitchName(
            tournament.Pitches.Select(p => p.Name),
            normalizedName);

        var pitch = new Pitch { Name = normalizedName };
        tournament.Pitches.Add(pitch);

        return Task.FromResult(ToPitchSummary(pitch));
    }

    public Task RenamePitchAsync(Guid tournamentId, Guid pitchId, string newName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        var pitch = tournament.Pitches.FirstOrDefault(p => p.Id == pitchId)
            ?? throw new InvalidOperationException("Pitch was not found.");

        var normalizedName = TournamentValidation.NormalizeName(newName);
        TournamentValidation.EnsureUniquePitchName(
            tournament.Pitches.Where(p => p.Id != pitchId).Select(p => p.Name),
            normalizedName);

        pitch.Name = normalizedName;
        return Task.CompletedTask;
    }

    public Task RemovePitchAsync(Guid tournamentId, Guid pitchId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        if (tournament.Pitches.Count <= 1)
        {
            throw new InvalidOperationException("Cannot remove the last pitch. A tournament must have at least one pitch.");
        }

        var removed = tournament.Pitches.RemoveAll(p => p.Id == pitchId);
        if (removed == 0)
        {
            throw new InvalidOperationException("Pitch was not found.");
        }

        return Task.CompletedTask;
    }

    public Task<TournamentSummary> StartTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        if (tournament.Status != TournamentStatus.Draft)
        {
            throw new InvalidOperationException("Only draft tournaments can be started.");
        }

        if (!tournament.TeamsConfirmed)
        {
            throw new InvalidOperationException("Confirm teams on the Teams page before starting the tournament.");
        }

        EnsureMinimumPitches(tournament);

        tournament.Status = TournamentStatus.Active;
        return Task.FromResult(ToSummary(tournament));
    }

    public Task<IReadOnlyList<GroupSummary>> GetGroupsAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        return Task.FromResult(ToGroupSummaries(tournament));
    }

    public Task<IReadOnlyList<GroupSummary>> GenerateGroupsAsync(
        Guid tournamentId,
        int groupCount,
        int teamsPerGroup,
        int minTeamsPerGroup,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        if (tournament.Status != TournamentStatus.Active)
        {
            throw new InvalidOperationException("Start the tournament before generating groups.");
        }

        var groupSizes = GroupDrawPlanner.CalculateGroupSizes(
            tournament.Teams.Count,
            groupCount,
            teamsPerGroup,
            minTeamsPerGroup);

        tournament.Groups.Clear();
        tournament.GroupStageSettings = new GroupStageSettings
        {
            GroupCount = groupCount,
            TeamsPerGroup = teamsPerGroup,
            MinTeamsPerGroup = minTeamsPerGroup
        };

        var shuffledTeams = tournament.Teams.OrderBy(_ => Random.Shared.Next()).ToList();
        var teamIndex = 0;

        for (var groupIndex = 0; groupIndex < groupCount; groupIndex++)
        {
            var group = new Group
            {
                Name = $"Group {(char)('A' + groupIndex)}"
            };

            for (var i = 0; i < groupSizes[groupIndex]; i++)
            {
                group.TeamIds.Add(shuffledTeams[teamIndex].Id);
                teamIndex++;
            }

            tournament.Groups.Add(group);
        }

        return Task.FromResult(ToGroupSummaries(tournament));
    }

    public Task<IReadOnlyList<GameSummary>> GetScheduleAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");
        return Task.FromResult(ToGameSummaries(tournament));
    }

    public Task<IReadOnlyList<GameSummary>> GenerateScheduleAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");
        if (tournament.Status != TournamentStatus.Active)
        {
            throw new InvalidOperationException("Start the tournament before generating the schedule.");
        }
        if (tournament.Groups.Count == 0)
        {
            throw new InvalidOperationException("Generate groups before generating the schedule.");
        }

        foreach (var game in GameScheduler.GenerateSchedule(tournament))
        {
            tournament.Schedule.Add(game);
        }

        return Task.FromResult(ToGameSummaries(tournament));
    }

    public Task<GameStatus> StartGameAsync(Guid tournamentId, Guid gameId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        var game = tournament.Schedule.FirstOrDefault(g => g.Id == gameId)
            ?? throw new InvalidOperationException("Game was not found.");

        if (game.Status != GameStatus.Scheduled)
        {
            throw new InvalidOperationException("Only scheduled games can be started.");
        }

        game.Status = GameStatus.Ongoing;
        return Task.FromResult(game.Status);
    }

    public Task<GameStatus> FinishGameAsync(Guid tournamentId, Guid gameId, int homeTeamScore, int awayTeamScore, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId)
            ?? throw new InvalidOperationException("Tournament was not found.");

        var game = tournament.Schedule.FirstOrDefault(g => g.Id == gameId)
            ?? throw new InvalidOperationException("Game was not found.");

        if (game.Status != GameStatus.Ongoing)
        {
            throw new InvalidOperationException("Only ongoing games can be finished.");
        }

        // Set the scores
        game.HomeTeamScore = homeTeamScore;
        game.AwayTeamScore = awayTeamScore;
        game.Status = GameStatus.Finished;

        // Update team statistics and award points
        var homeTeamId = game.Teams.Item1;
        var awayTeamId = game.Teams.Item2;

        var homeTeam = tournament.Teams.FirstOrDefault(t => t.Id == homeTeamId);
        var awayTeam = tournament.Teams.FirstOrDefault(t => t.Id == awayTeamId);

        // TODO: This should be in domain
        if (homeTeam != null && awayTeam != null)
        {
            // Update goals
            homeTeam.Goals += homeTeamScore;
            homeTeam.ConcededGoals += awayTeamScore;

            awayTeam.Goals += awayTeamScore;
            awayTeam.ConcededGoals += homeTeamScore;

            // Award points: 3 for win, 1 for draw
            if (homeTeamScore > awayTeamScore)
            {
                // Home team wins
                homeTeam.Points += 3;
            }
            else if (awayTeamScore > homeTeamScore)
            {
                // Away team wins
                awayTeam.Points += 3;
            }
            else
            {
                // Draw
                homeTeam.Points += 1;
                awayTeam.Points += 1;
            }
        }

        return Task.FromResult(game.Status);
    }

    public Task DeleteAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var removed = _tournaments.RemoveAll(t => t.Id == tournamentId);
        if (removed == 0)
        {
            throw new InvalidOperationException("Tournament was not found.");
        }

        return _persistenceService.DeleteTournamentAsync(tournamentId, cancellationToken);
    }

    public Task SaveAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournament = FindTournament(tournamentId);

        if (tournament is null)
        {
            throw new InvalidOperationException("Tournament was not found.");
        }

        return _persistenceService.SaveTournamentAsync(tournament, cancellationToken);
    }

    private Tournament? FindTournament(Guid tournamentId)
        => _tournaments.FirstOrDefault(t => t.Id == tournamentId);

    private static void EnsureMinimumPitches(Tournament tournament)
    {
        if (tournament.Pitches.Count == 0)
        {
            tournament.Pitches.Add(new Pitch { Name = "Pitch 1" });
        }
    }

    private static TournamentSummary ToSummary(Tournament tournament) => new()
    {
        Id = tournament.Id,
        Name = tournament.Name,
        StartDate = tournament.StartDate,
        Sport = tournament.Sport,
        Status = tournament.Status,
        PitchCount = tournament.Pitches.Count,
        TeamCount = tournament.Teams.Count,
        HasGroups = tournament.Groups.Count > 0,
        TeamsConfirmed = tournament.TeamsConfirmed
    };

    private static IReadOnlyList<GroupSummary> ToGroupSummaries(Tournament tournament)
    {
        var teamNames = tournament.Teams.ToDictionary(t => t.Id, t => t.Name);

        return tournament.Groups
            .Select(group => new GroupSummary
            {
                Id = group.Id,
                Name = group.Name,
                TeamNames = group.TeamIds
                    .Select(id => teamNames.TryGetValue(id, out var name) ? name : "Unknown team")
                    .ToList()
            })
            .ToList();
    }

    private static IReadOnlyList<GameSummary> ToGameSummaries(Tournament tournament)
    {
        var teamNames = tournament.Teams.ToDictionary(t => t.Id, t => t.Name);
        var pitchNames = tournament.Pitches.ToDictionary(p => p.Id, p => p.Name);
        return tournament.Schedule
            .Select(game => new GameSummary
            {
                Id = game.Id,
                Name = game.Name,
                PitchName = pitchNames.TryGetValue(game.PitchId, out var pitchName) ? pitchName : "Unknown pitch",
                HomeTeamName = teamNames.TryGetValue(game.Teams.Item1, out var homeTeamName) ? homeTeamName : "Unknown team",
                AwayTeamName = teamNames.TryGetValue(game.Teams.Item2, out var awayTeamName) ? awayTeamName : "Unknown team",
                Status = game.Status,
                HomeTeamScore = game.HomeTeamScore,
                AwayTeamScore = game.AwayTeamScore
            })
            .ToList();
    }

    private static TeamSummary ToTeamSummary(Team team) => new()
    {
        Id = team.Id,
        Name = team.Name
    };

    private static PitchSummary ToPitchSummary(Pitch pitch) => new()
    {
        Id = pitch.Id,
        Name = pitch.Name
    };
}
