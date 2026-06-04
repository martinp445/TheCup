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
            }
        }

        return new Game
        {
            Id = id,
            Name = name,
            PitchId = pitchId,
            Teams = teams
        };
    }

    public override void Write(Utf8JsonWriter writer, Game value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
