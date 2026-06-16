
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GorodTV.Core.Converters;

/// <summary>Читает long и из числа, и из строки ("1781471340" или 1781471340).</summary>
public class FlexibleLongConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt64(),
            JsonTokenType.String => long.TryParse(reader.GetString(), out var v) ? v : 0,
            _ => 0,
        };

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}
