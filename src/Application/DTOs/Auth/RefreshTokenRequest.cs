using System.ComponentModel.DataAnnotations;

namespace UrbanInfraSystem.Application.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string AccessToken { get; set; } = default!;

    [Required]
    public string RefreshToken { get; set; } = default!;
}
