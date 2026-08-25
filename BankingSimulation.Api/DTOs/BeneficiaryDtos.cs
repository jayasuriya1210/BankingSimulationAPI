using System.ComponentModel.DataAnnotations;

namespace BankingSimulation.Api.DTOs;

public record AddBeneficiaryRequest(
    [Required, MaxLength(34)] string AccountNumber,
    [Required, MaxLength(200)] string Name);

public record BeneficiaryResponse(
    long Id,
    string AccountNumber,
    string Name,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc);

public record ReviewBeneficiaryRequest([Required] string Decision, string? Reason);
