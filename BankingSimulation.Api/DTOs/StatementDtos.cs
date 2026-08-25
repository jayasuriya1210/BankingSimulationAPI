using System.ComponentModel.DataAnnotations;

namespace BankingSimulation.Api.DTOs;

public record StatementRequest(
    [Required] long AccountId,
    [Required] DateTime From,
    [Required] DateTime To);
