using System.ComponentModel.DataAnnotations;

namespace BankingSimulation.Api.DTOs;

public record AccountResponse(
    long Id,
    string AccountNumber,
    long OwnerId,
    string OwnerName,
    decimal Balance,
    decimal DailyTransferLimit,
    string Status,
    DateTime CreatedAtUtc);

public record FreezeAccountRequest(string? Reason);

public record CreateAccountRequest(
    [Required] long OwnerId,
    [Required, Range(0.01, double.MaxValue)] decimal DailyTransferLimit);
