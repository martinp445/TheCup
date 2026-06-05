using System.Text.Json;
using System.Text.Json.Serialization;
using TheCup_Domain.Entities;

namespace TheCup_Infrastructure.Services;

/// <summary>
/// Custom JSON converter for Game entity that properly handles
/// the Teams ValueTuple property.
/// </summary>
public class GameJsonConverter : JsonConverter<Game>
{
    public override Game? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        var id = Guid.Empty;
        var name = "Game";
        var pitchId = Guid.Empty;
        var teams = new ValueTuple<Guid, Guid>(Guid.Empty, Guid.Empty);
        var status = 0;
        var homeTeamScore = 0;
        var awayTeamScore = 0;

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
                    name = reader.GetString() ?? "Game";
                    break;
                case "pitchid":
                    pitchId = JsonSerializer.Deserialize<Guid>(ref reader, options);
                    break;
                case "teams":
                    teams = JsonSerializer.Deserialize<(Guid, Guid)>(ref reader, options);
                    break;
                case "status":
                    status = reader.GetInt32();
                    break;
                case "hometeamscore":
                    homeTeamScore = reader.GetInt32();
                    break;
                case "awayteamscore":
                    awayTeamScore = reader.GetInt32();
                    break;
            }
        }

        return new Game
        {
            Id = id,
            Name = name,
            PitchId = pitchId,
            Teams = teams,
            Status = (global::TheCup_Domain.Enums.GameStatus)status,
            HomeTeamScore = homeTeamScore,
            AwayTeamScore = awayTeamScore
        };
    }

    public override void Write(Utf8JsonWriter writer, Game value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
