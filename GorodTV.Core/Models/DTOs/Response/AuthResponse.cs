using System.Text.Json.Serialization;

namespace GorodTV.Core.Models.DTOs.Response;

public record AuthResponse(
    [property: JsonPropertyName("sessionId")] string? SessionId
);
