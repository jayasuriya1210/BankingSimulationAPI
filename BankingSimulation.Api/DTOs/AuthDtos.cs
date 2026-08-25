using System.ComponentModel.DataAnnotations;

namespace BankingSimulation.Api.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record SignupRequest(
    [Required, MaxLength(200)] string DisplayName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);

public record RefreshTokenRequest([Required] string RefreshToken);

public record TokenResponse(string AccessToken, string RefreshToken, string Role, long UserId);
