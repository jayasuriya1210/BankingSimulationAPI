using BankingSimulation.Api.DTOs;
using BankingSimulation.Api.Extensions;
using BankingSimulation.Api.Middleware;
using BankingSimulation.Api.Models;
using BankingSimulation.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSimulation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/statements")]
public class StatementController(StatementService statementService) : ControllerBase
{
    private string? CorrelationId => HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();

    [HttpGet]
    public async Task<IActionResult> GetStatement([FromQuery] StatementRequest req)
    {
        var result = await statementService.GenerateAsync(User.GetUserId(), req, User.GetRole());
        return Ok(ApiResponse.Ok("Statement retrieved.", result, CorrelationId));
    }
}
