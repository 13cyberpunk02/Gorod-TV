using GorodTV.Core.Converters;
using System.Text.Json.Serialization;

namespace GorodTV.Core.Models.DTOs.Response;

// сервер отдаёт {"unixtime":"1781471340"} — строкой, поэтому гибкий конвертер
public record UnixTimeResponse(
    [property: JsonPropertyName("unixtime")]
    [property: JsonConverter(typeof(FlexibleLongConverter))]
    long UnixTime
)
{
    public long Value => UnixTime > 0 ? UnixTime : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
