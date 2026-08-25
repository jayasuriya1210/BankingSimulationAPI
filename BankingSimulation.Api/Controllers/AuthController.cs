using BankingSimulation.Api.DTOs;
using BankingSimulation.Api.Middleware;
using BankingSimulation.Api.Models;
using BankingSimulation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingSimulation.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    private string? CorrelationId => HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await authService.LoginAsync(req, CorrelationId);
        return Ok(ApiResponse.Ok("Login successful.", result, CorrelationId));
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequest req)
    {
        var result = await authService.SignupAsync(req, CorrelationId);
        return StatusCode(201, ApiResponse.Ok("Registration successful.", result, CorrelationId));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest req)
    {
        var result = await authService.RefreshAsync(req.RefreshToken, CorrelationId);
        return Ok(ApiResponse.Ok("Token refreshed.", result, CorrelationId));
    }
}
