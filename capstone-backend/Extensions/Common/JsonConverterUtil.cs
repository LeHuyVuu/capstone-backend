using System.Text.Json;
using System.Text.Json.Serialization;

namespace capstone_backend.Extensions.Common
{
    /// <summary>
    /// Custom JSON converter to ensure all DateTime values are serialized as UTC with 'Z' suffix
    /// This prevents timezone inconsistencies across different API endpoints
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateTime = reader.GetDateTime();
            
            // If the DateTime doesn't have a Kind specified, assume it's UTC
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            
            // Convert to UTC if it's local time
            return dateTime.ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Always convert to UTC before serializing
            var utcDateTime = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                _ => value
            };
            
            // Write in ISO 8601 format with 'Z' suffix (e.g., "2026-04-30T06:00:00Z")
            writer.WriteStringValue(utcDateTime);
        }
    }

    /// <summary>
    /// Custom JSON converter for nullable DateTime values
    /// </summary>
    public class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            
            var dateTime = reader.GetDateTime();
            
            // If the DateTime doesn't have a Kind specified, assume it's UTC
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            
            // Convert to UTC if it's local time
            return dateTime.ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }
            
            // Always convert to UTC before serializing
            var utcDateTime = value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
                _ => value.Value
            };
            
            // Write in ISO 8601 format with 'Z' suffix (e.g., "2026-04-30T06:00:00Z")
            writer.WriteStringValue(utcDateTime);
        }
    }

    public static class JsonConverterUtil
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new UtcDateTimeConverter(),
                new UtcNullableDateTimeConverter()
            }
        };

        /// <summary>
        /// Deserialize json to object
        /// - If null/empty/invalid then json return fallback (default is new())
        /// </summary>
        public static T DeserializeOrDefault<T>(string? json, T? fallback = default) where T : class, new()
        {
            if (string.IsNullOrEmpty(json))
                return fallback ?? new T();

            try
            {
                return JsonSerializer.Deserialize<T>(json, Options) ?? (fallback ?? new T());
            }
            catch
            {
                return fallback ?? new T();
            }
        }

        /// <summary>
        /// Serialize object to json (not null)
        /// </summary>
        public static string Serialize<T>(T data)
        {
            return JsonSerializer.Serialize(data, Options);
        }

        /// <summary>
        /// Parse image list from database JSON string format
        /// Handles formats like: '["url1", "url2"]' or ["url1", "url2"]
        /// </summary>
        public static List<string>? ParseImageList(string? imageString)
        {
            if (string.IsNullOrWhiteSpace(imageString))
                return null;

            try
            {
                // Remove outer quotes first
                var trimmed = imageString.Trim();

                // Remove leading/trailing single or double quotes
                if ((trimmed.StartsWith("'") && trimmed.EndsWith("'")) ||
                    (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")))
                {
                    trimmed = trimmed.Substring(1, trimmed.Length - 2);
                }

                // Now check if it's a JSON array
                trimmed = trimmed.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    var deserializedArray = JsonSerializer.Deserialize<List<string>>(trimmed, Options);
                    if (deserializedArray != null && deserializedArray.Any())
                    {
                        return deserializedArray
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => s.Trim())
                            .ToList();
                    }
                }
            }
            catch
            {
                // Fallback: parse as comma-separated string
                return imageString
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().Trim('\'', '"', '[', ']', '\\'))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            return null;
        }
    }
}
