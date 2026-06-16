
using System.Text.Json.Serialization;

namespace GorodTV.Core.Models.DTOs.Response;

public record EpgResponse(
    [property: JsonPropertyName("epg")] IReadOnlyList<EpgItemDto>? Epg
);

public record EpgItemDto(
    [property: JsonPropertyName("caption")] string Caption,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("start_time")] string StartTime,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("record")] bool Record
);
