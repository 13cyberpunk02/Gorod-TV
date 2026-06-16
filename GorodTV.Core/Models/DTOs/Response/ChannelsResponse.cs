using System.Text.Json.Serialization;

namespace GorodTV.Core.Models.DTOs.Response;

public record ChannelsResponse(
    [property: JsonPropertyName("channels")] IReadOnlyList<ChannelDto>? Channels
);

public record ChannelDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("epgId")] string EpgId,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("iconsvg")] string IconSvg,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("link")] string Link
);
