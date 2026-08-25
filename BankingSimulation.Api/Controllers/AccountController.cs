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
[Route("api/accounts")]
public class AccountController(AccountService accountService, IDepositService depositService) : ControllerBase
{
    private string? CorrelationId => HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();

    [HttpPost("create")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest req)
    {
        var result = await accountService.CreateAccountAsync(User.GetUserId(), req, CorrelationId);
        return StatusCode(201, ApiResponse.Ok("Account created.", result, CorrelationId));
    }

    [HttpPost("{id:long}/deposit")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Deposit(long id, [FromBody] DepositRequest req)
    {
        var result = await depositService.DepositAsync(User.GetUserId(), id, req, CorrelationId);
        return StatusCode(201, ApiResponse.Ok("Deposit completed.", result, CorrelationId));
    }

    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyAccounts()
    {
        var result = await accountService.GetMyAccountsAsync(User.GetUserId());
        return Ok(ApiResponse.Ok("Accounts retrieved.", result, CorrelationId));
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "Admin, Staff")]
    public async Task<IActionResult> GetAccount(long id)
    {
        var result = await accountService.GetAccountAsync(id, User.GetUserId(), User.GetRole());
        return Ok(ApiResponse.Ok("Account retrieved.", result, CorrelationId));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetAllAccounts()
    {
        var result = await accountService.GetAllAccountsAsync();
        return Ok(ApiResponse.Ok("Accounts retrieved.", result, CorrelationId));
    }

    [HttpPatch("{id:long}/freeze")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Freeze(long id, [FromBody] FreezeAccountRequest req)
    {
        var result = await accountService.FreezeAccountAsync(id, User.GetUserId(), req.Reason, CorrelationId);
        return Ok(ApiResponse.Ok("Account frozen.", result, CorrelationId));
    }

    [HttpPatch("{id:long}/unfreeze")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Unfreeze(long id)
    {
        var result = await accountService.UnfreezeAccountAsync(id, User.GetUserId(), CorrelationId);
        return Ok(ApiResponse.Ok("Account unfrozen.", result, CorrelationId));
    }
}
