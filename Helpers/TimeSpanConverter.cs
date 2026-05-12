using System.Text.Json;
using System.Text.Json.Serialization;

namespace turf_management_system.Helpers
{
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value)) return TimeSpan.Zero;

            if (TimeSpan.TryParse(value, out var result))
            {
                return result;
            }

            // Handle HH:mm format if TryParse fails (though it usually works)
            if (value.Contains(":") && value.Split(':').Length == 2)
            {
                if (TimeSpan.TryParse(value + ":00", out var result2))
                    return result2;
            }

            return TimeSpan.Zero;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(@"hh\:mm"));
        }
    }
}
