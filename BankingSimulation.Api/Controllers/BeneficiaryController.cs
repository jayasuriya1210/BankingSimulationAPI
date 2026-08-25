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
[Route("api/beneficiaries")]
public class BeneficiaryController(BeneficiaryService beneficiaryService) : ControllerBase
{
    private string? CorrelationId => HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Add([FromBody] AddBeneficiaryRequest req)
    {
        var result = await beneficiaryService.AddAsync(User.GetUserId(), req, CorrelationId);
        return StatusCode(201, ApiResponse.Ok("Beneficiary added.", result, CorrelationId));
    }

    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMy()
    {
        var result = await beneficiaryService.GetMyBeneficiariesAsync(User.GetUserId());
        return Ok(ApiResponse.Ok("Beneficiaries retrieved.", result, CorrelationId));
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetPending()
    {
        var result = await beneficiaryService.GetPendingAsync();
        return Ok(ApiResponse.Ok("Pending beneficiaries retrieved.", result, CorrelationId));
    }

    [HttpPatch("{id:long}/review")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Review(long id, [FromBody] ReviewBeneficiaryRequest req)
    {
        var result = await beneficiaryService.ReviewAsync(id, User.GetUserId(), req, CorrelationId);
        return Ok(ApiResponse.Ok("Beneficiary reviewed.", result, CorrelationId));
    }
}
