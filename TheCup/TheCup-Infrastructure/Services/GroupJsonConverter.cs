using System.Text.Json;
using System.Text.Json.Serialization;
using TheCup_Domain.Entities;

namespace TheCup_Infrastructure.Services;

/// <summary>
/// Custom JSON converter for Group entity that properly handles
/// read-only TeamIds collection.
/// </summary>
public class GroupJsonConverter : JsonConverter<Group>
{
    public override Group? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        var id = Guid.Empty;
        var name = string.Empty;
        var teamIds = new List<Guid>();

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
                case "teamids":
                    teamIds = JsonSerializer.Deserialize<List<Guid>>(ref reader, options) ?? new List<Guid>();
                    break;
            }
        }

        var group = new Group
        {
            Id = id,
            Name = name
        };

        // Populate the read-only TeamIds collection
        group.TeamIds.AddRange(teamIds);

        return group;
    }

    public override void Write(Utf8JsonWriter writer, Group value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
