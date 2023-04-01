using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace microservices_dashboard_api.JsonConverters;

public class HealthReportEntryJsonConverter : JsonConverter<HealthReportEntry>
{
    HealthStatus status = default;
    string? description = default;
    TimeSpan duration;
    Exception? exception = default;
    IReadOnlyDictionary<string, object>? data = default;
    List<string>? tags = default;

    public override HealthReportEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new HealthReportEntry(status, description, duration, exception, data, tags);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("JsonTokenType was not PropertyName");
            }

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new JsonException("Failed to get property name");
            }


            if (string.Equals(propertyName, nameof(HealthReportEntry.Status), StringComparison.InvariantCultureIgnoreCase))
            {
                reader.Read();
                status = (HealthStatus)reader.GetInt16();
            }
            else if (string.Equals(propertyName, nameof(HealthReportEntry.Description), StringComparison.InvariantCultureIgnoreCase))
            {
                reader.Read();
                description = reader.GetString();
            }
            else if (string.Equals(propertyName, nameof(HealthReportEntry.Duration), StringComparison.InvariantCultureIgnoreCase))
            {
                reader.Read();
                duration = TimeSpan.Parse(reader.GetString()!);
            }
            else if (string.Equals(propertyName, nameof(HealthReportEntry.Exception), StringComparison.InvariantCultureIgnoreCase))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.Null)
                {
                    //reader.Read();
                }
            }
            else if (string.Equals(propertyName, nameof(HealthReportEntry.Data), StringComparison.InvariantCultureIgnoreCase))
            {
                reader.Read();
                reader.Read();
            }
            else if (string.Equals(propertyName, nameof(HealthReportEntry.Tags), StringComparison.InvariantCultureIgnoreCase))
            {
                reader.Read();
                if (reader.TokenType == JsonTokenType.Null)
                {
                }
                else if (reader.TokenType == JsonTokenType.StartArray)
                {
                    tags = new List<string>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            tags.Add(reader.GetString());
                        }
                        else if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            break;
                        }
                    }
                }
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, HealthReportEntry healthReport, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}