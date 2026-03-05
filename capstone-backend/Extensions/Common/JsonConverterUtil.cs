using System.Text.Json;
using System.Text.Json.Serialization;

namespace capstone_backend.Extensions.Common
{
    public static class JsonConverterUtil
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
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
    }
}
