using System.ComponentModel.DataAnnotations;

namespace BankingSimulation.Api.DTOs;

public record TransferRequest(
    [Required] long FromAccountId,
    [Required] long ToAccountId,
    [Required, Range(0.01, double.MaxValue)] decimal Amount,
    [MaxLength(200)] string? Description,
    [MaxLength(50)] string? IdempotencyKey);

public record DepositRequest(
    [Required, Range(0.01, double.MaxValue)] decimal Amount,
    [MaxLength(200)] string? Description,
    [MaxLength(50)] string? IdempotencyKey);

public record TransactionResponse(
    long Id,
    string Reference,
    long? FromAccountId,
    long? ToAccountId,
    decimal Amount,
    string Type,
    string Status,
    string? Description,
    DateTime CreatedAtUtc);

public record TransactionFilterRequest(
    DateTime? From,
    DateTime? To,
    string? Type,
    decimal? MinAmount,
    decimal? MaxAmount,
    int Page = 1,
    int PageSize = 20);
