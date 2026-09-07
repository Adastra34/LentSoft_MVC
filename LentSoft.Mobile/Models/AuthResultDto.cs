using System.Text.Json.Serialization;

namespace LentSoft.Mobile.Models;

public class AuthResultDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public UserDto? User { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
