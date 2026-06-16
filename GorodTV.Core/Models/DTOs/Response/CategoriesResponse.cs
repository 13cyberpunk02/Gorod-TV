using System.Text.Json.Serialization;

namespace GorodTV.Core.Models.DTOs.Response;

public record CategoriesResponse(
    [property: JsonPropertyName("categories")] IReadOnlyList<CategoryDto>? Categories
);

public record CategoryDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("icon")] string Icon
);
