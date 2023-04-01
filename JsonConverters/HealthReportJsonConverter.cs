using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace microservices_dashboard_api.JsonConverters;

public class HealthReportJsonConverter : JsonConverter<HealthReport>
{
    public override HealthReport Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        Dictionary<string, HealthReportEntry> entries = new Dictionary<string, HealthReportEntry>();
        HealthStatus status = default;
        TimeSpan totalDuration = default;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new HealthReport(entries, status, totalDuration);
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


            if (string.Equals(propertyName, nameof(HealthReport.Entries), StringComparison.InvariantCultureIgnoreCase))
            {

                var HealthReportEntryJsonConverter = options.Converters.FirstOrDefault(x => x.GetType() == typeof(HealthReportEntryJsonConverter)) as JsonConverter<HealthReportEntry>;



                while (reader.Read())
                {
                    string entryKey = string.Empty;
                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        reader.Read();
                    }
                    else if (reader.TokenType == JsonTokenType.EndObject) { break; }


                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        entryKey = reader.GetString();
                        reader.Read();
                    }

                    var entry = HealthReportEntryJsonConverter.Read(ref reader, typeof(HealthReportEntry), options);

                    entries.Add(entryKey!, entry);
                }
                //var a = ExtractValue(ref reader, options);
            }
            else if (string.Equals(propertyName, nameof(HealthReport.Status), StringComparison.InvariantCultureIgnoreCase))
            {
                reader.Read();
                status = (HealthStatus)reader.GetInt16();

            }
            else if (string.Equals(propertyName, nameof(HealthReport.TotalDuration), StringComparison.InvariantCultureIgnoreCase))
            {
                reader.Read();
                totalDuration = TimeSpan.Parse(reader.GetString()!);

            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, HealthReport healthReport, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
