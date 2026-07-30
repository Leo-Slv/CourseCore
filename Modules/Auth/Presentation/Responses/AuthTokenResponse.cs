using System.Text.Json.Serialization;

namespace CourseCore.Api.Modules.Auth.Presentation.Responses;

public class AuthTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RefreshToken { get; init; }

    public DateTime ExpiresAt { get; init; }
}
