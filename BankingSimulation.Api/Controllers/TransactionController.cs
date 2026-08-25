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
[Route("api/transactions")]
public class TransactionController(TransactionService transactionService) : ControllerBase
{
    private string? CorrelationId => HttpContext.Items[CorrelationIdMiddleware.HeaderName]?.ToString();

    [HttpGet("account/{accountId:long}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyTransactions(long accountId, [FromQuery] TransactionFilterRequest filter)
    {
        var result = await transactionService.GetMyTransactionsAsync(User.GetUserId(), accountId, filter);
        return Ok(ApiResponse.Ok("Transactions retrieved.", result, CorrelationId));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetAll([FromQuery] TransactionFilterRequest filter)
    {
        var result = await transactionService.GetAllTransactionsAsync(filter);
        return Ok(ApiResponse.Ok("Transactions retrieved.", result, CorrelationId));
    }
}
