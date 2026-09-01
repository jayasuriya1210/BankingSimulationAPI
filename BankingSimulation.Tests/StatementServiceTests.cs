using System;
using System.Threading.Tasks;
using BankingSimulation.Api.DTOs;
using BankingSimulation.Api.Services;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;
using Moq;
using Xunit;

namespace BankingSimulation.Tests;

public class StatementServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<ITransactionRepository> _txRepo = new();
    private readonly StatementService _sut;

    public StatementServiceTests()
    {
        _sut = new StatementService(_accountRepo.Object, _txRepo.Object);
    }

    [Fact]
    public async Task GenerateAsync_RequestedRangeBeforeAccountCreation_ThrowsArgumentException()
    {
        var account = new Account { Id = 1, OwnerId = 10, CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        _accountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);

        var req = new StatementRequest(
            1,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc)
        );

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GenerateAsync(10, req, "Customer"));
    }

    [Fact]
    public async Task GenerateAsync_FutureRange_ThrowsArgumentException()
    {
        var account = new Account { Id = 1, OwnerId = 10, CreatedAtUtc = DateTime.UtcNow.AddYears(-1) };
        _accountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);

        var req = new StatementRequest(
            1,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2)
        );

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GenerateAsync(10, req, "Customer"));
    }
}
