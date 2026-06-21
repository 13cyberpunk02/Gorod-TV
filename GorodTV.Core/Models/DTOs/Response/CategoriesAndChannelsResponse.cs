
using System.Text.Json.Serialization;

namespace GorodTV.Core.Models.DTOs.Response;

// Ответ единого эндпоинта categoriesAndChannels: категории + вложенные каналы.
// Текущей программы в ответе НЕТ — она догружается отдельно (лениво), как в избранном.
public record CategoriesAndChannelsResponse(
    [property: JsonPropertyName("categoriesAndChannels")]
    IReadOnlyList<CategoryWithChannelsDto>? CategoriesAndChannels
);

public record CategoryWithChannelsDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("channelsList")]
    IReadOnlyList<ChannelInCategoryDto>? ChannelsList
);

public record ChannelInCategoryDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("category")] string Category, 
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("epgId")] string EpgId,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("iconsvg")] string IconSvg,
    [property: JsonPropertyName("link")] string Link
);
