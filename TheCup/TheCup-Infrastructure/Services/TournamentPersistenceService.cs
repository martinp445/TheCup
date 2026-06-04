using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using TheCup_Domain.Entities;
using TheCup_Infrastructure.Enviroment;

namespace TheCup_Infrastructure.Services;

public sealed class TournamentPersistenceService : ITournamentPersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = 
        { 
            new TournamentJsonConverter(),
            new GroupJsonConverter(),
            new GameJsonConverter()
        }
    };

    public Task SaveTournamentAsync(Tournament tournament, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string filePath = FolderUtilities.TournamentJsonFilePath(tournament.Id);
        var json = JsonSerializer.Serialize(tournament, JsonOptions);
        File.WriteAllText(filePath, json);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Tournament>> LoadTournamentsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tournaments = new List<Tournament>();
        string folder = FolderUtilities.GetProgramDataFolder();

        if (!Directory.Exists(folder))
        {
            return Task.FromResult<IReadOnlyList<Tournament>>(tournaments.AsReadOnly());
        }

        var jsonFiles = Directory.GetFiles(folder, "*.json");

        foreach (var filePath in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var tournament = JsonSerializer.Deserialize<Tournament>(json, JsonOptions);

                if (tournament is not null)
                {
                    tournaments.Add(tournament);
                }
            }
            catch (Exception ex)
            {
                // Log or handle deserialization errors gracefully
                System.Diagnostics.Debug.WriteLine($"Failed to load tournament from {filePath}: {ex.Message}");
            }
        }

        return Task.FromResult<IReadOnlyList<Tournament>>(tournaments.AsReadOnly());
    }
}





