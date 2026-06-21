
using GorodTV.Core.Models.DTOs.Request;
using GorodTV.Core.Models.DTOs.Response;
using System.Text.Json.Serialization;

namespace GorodTV.Core.Services;

// Source-generated JSON. Перечисли ВСЕ типы, что (де)сериализуются по сети.
// AuthResponse раньше отсутствовал -> вход падал (NotSupportedException).
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
// --- Response ---
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(CategoriesAndChannelsResponse))]
[JsonSerializable(typeof(CategoryWithChannelsDto))]
[JsonSerializable(typeof(ChannelInCategoryDto))]
[JsonSerializable(typeof(CategoriesResponse))]
[JsonSerializable(typeof(ChannelsResponse))]
[JsonSerializable(typeof(EpgResponse))]
[JsonSerializable(typeof(UnixTimeResponse))]
// --- Request ---
[JsonSerializable(typeof(LoginRequest))]
public partial class GorodTvJsonContext : JsonSerializerContext
{
}

