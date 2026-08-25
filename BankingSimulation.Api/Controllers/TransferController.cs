using BankingSimulation.Api.DTOs;
using BankingSimulation.Api.Extensions;
using BankingSimulation.Api.Middleware;
using BankingSimulation.Api.Models;
using BankingSimulation.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingSimulation.Api.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/transfers")]
public class TransferController(TransferService transferService) : ControllerBase
{
    private string? CorrelationId => HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();

    [HttpPost]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest req)
    {
        var result = await transferService.TransferAsync(User.GetUserId(), req, CorrelationId);
        return StatusCode(201, ApiResponse.Ok("Transfer completed.", result, CorrelationId));
    }
}
