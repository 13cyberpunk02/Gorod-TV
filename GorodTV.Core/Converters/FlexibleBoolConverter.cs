
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GorodTV.Core.Converters;

/// <summary>
/// Терпимый парсер bool: принимает true/false, "true"/"false", "1"/"0", 1/0.
/// API иногда отдаёт булевы строкой или числом — чтобы не падать на десериализации.
/// </summary>
public class FlexibleBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.GetInt32() != 0,
            JsonTokenType.String => ParseString(reader.GetString()),
            JsonTokenType.Null => false,
            _ => false,
        };

    private static bool ParseString(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        if (bool.TryParse(s, out var b)) return b;
        if (int.TryParse(s, out var n)) return n != 0;
        return s.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || s.Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);
}
