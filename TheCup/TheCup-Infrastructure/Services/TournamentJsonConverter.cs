using System.Text.Json;
using System.Text.Json.Serialization;
using TheCup_Domain.Entities;
using TheCup_Domain.ValueObjects;

namespace TheCup_Infrastructure.Services;

/// <summary>
/// Custom JSON converter for Tournament entity that properly handles
/// read-only collections (Teams, Pitches, Groups, Schedule).
/// </summary>
public class TournamentJsonConverter : JsonConverter<Tournament>
{
    public override Tournament? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        var id = Guid.Empty;
        var name = string.Empty;
        var startDate = DateOnly.FromDateTime(DateTime.Today);
        var sport = 0; // Default to Football (0)
        var status = 0; // Default to Draft (0)
        var teamsConfirmed = false;
        var teams = new List<Team>();
        var pitches = new List<Pitch>();
        var groups = new List<Group>();
        var schedule = new List<Game>();
        GroupStageSettings? groupStageSettings = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var propertyName = reader.GetString();

            reader.Read();

            switch (propertyName?.ToLowerInvariant())
            {
                case "id":
                    id = JsonSerializer.Deserialize<Guid>(ref reader, options);
                    break;
                case "name":
                    name = reader.GetString() ?? string.Empty;
                    break;
                case "startdate":
                    startDate = JsonSerializer.Deserialize<DateOnly>(ref reader, options);
                    break;
                case "sport":
                    sport = reader.GetInt32();
                    break;
                case "status":
                    status = reader.GetInt32();
                    break;
                case "teamsconfirmed":
                    teamsConfirmed = reader.GetBoolean();
                    break;
                case "teams":
                    teams = JsonSerializer.Deserialize<List<Team>>(ref reader, options) ?? new List<Team>();
                    break;
                case "pitches":
                    pitches = JsonSerializer.Deserialize<List<Pitch>>(ref reader, options) ?? new List<Pitch>();
                    break;
                case "groups":
                    groups = JsonSerializer.Deserialize<List<Group>>(ref reader, options) ?? new List<Group>();
                    break;
                case "schedule":
                    schedule = JsonSerializer.Deserialize<List<Game>>(ref reader, options) ?? new List<Game>();
                    break;
                case "groupstagesettings":
                    groupStageSettings = JsonSerializer.Deserialize<GroupStageSettings>(ref reader, options);
                    break;
            }
        }

        var tournament = new Tournament
        {
            Id = id,
            Name = name,
            StartDate = startDate,
            Sport = (global::TheCup_Domain.Enums.Sport)sport,
            Status = (global::TheCup_Domain.Enums.TournamentStatus)status,
            TeamsConfirmed = teamsConfirmed,
            GroupStageSettings = groupStageSettings
        };

        // Populate the read-only collections
        tournament.Teams.AddRange(teams);
        tournament.Pitches.AddRange(pitches);
        tournament.Groups.AddRange(groups);
        tournament.Schedule.AddRange(schedule);

        return tournament;
    }

    public override void Write(Utf8JsonWriter writer, Tournament value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
