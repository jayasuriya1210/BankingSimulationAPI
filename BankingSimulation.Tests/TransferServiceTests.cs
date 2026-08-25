using BankingSimulation.Api.DTOs;
using BankingSimulation.Api.Services;
using BankingSimulation.Data.Database;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BankingSimulation.Tests;

public class TransferServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
    private readonly Mock<ITransactionRepository> _transactionRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();
    private readonly BankingDbContext _db;
    private readonly TransferService _sut;

    public TransferServiceTests()
    {
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BankingDbContext(options);
        var auditService = new AuditService(_auditRepo.Object);
        _sut = new TransferService(_accountRepo.Object, _beneficiaryRepo.Object,
            _transactionRepo.Object, auditService, _db);
    }

    private static Account MakeAccount(long id, long ownerId, decimal balance,
        decimal dailyLimit = 5000m, AccountStatus status = AccountStatus.Active) =>
        new() { Id = id, AccountNumber = $"ACC{id:010}", OwnerId = ownerId,
                Balance = balance, DailyTransferLimit = dailyLimit, Status = status };

    [Fact]
    public async Task Transfer_InsufficientBalance_Throws()
    {
        var from = MakeAccount(1, 10, 100m);
        var to   = MakeAccount(2, 20, 0m);
        _accountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(from);
        _accountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(to);
        _transactionRepo.Setup(r => r.GetByReferenceAsync(It.IsAny<string>())).ReturnsAsync((Transaction?)null);
        _transactionRepo.Setup(r => r.GetDailyOutgoingTotalAsync(1, It.IsAny<DateTime>())).ReturnsAsync(0m);
        _beneficiaryRepo.Setup(r => r.GetByOwnerIdAsync(10)).ReturnsAsync(
            new[] { new Beneficiary { OwnerId = 10, BeneficiaryAccountNumber = "ACC0000000002", Status = BeneficiaryStatus.Approved } });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.TransferAsync(10, new TransferRequest(1, 2, 500m, null, null), null));
    }

    [Fact]
    public async Task Transfer_FrozenSourceAccount_Throws()
    {
        var from = MakeAccount(1, 10, 5000m, 5000m, AccountStatus.Frozen);
        var to   = MakeAccount(2, 20, 0m);
        _accountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(from);
        _accountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(to);
        _transactionRepo.Setup(r => r.GetByReferenceAsync(It.IsAny<string>())).ReturnsAsync((Transaction?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.TransferAsync(10, new TransferRequest(1, 2, 100m, null, null), null));
    }

    [Fact]
    public async Task Transfer_DailyLimitExceeded_Throws()
    {
        var from = MakeAccount(1, 10, 10000m, 5000m);
        var to   = MakeAccount(2, 20, 0m);
        _accountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(from);
        _accountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(to);
        _transactionRepo.Setup(r => r.GetByReferenceAsync(It.IsAny<string>())).ReturnsAsync((Transaction?)null);
        _transactionRepo.Setup(r => r.GetDailyOutgoingTotalAsync(1, It.IsAny<DateTime>())).ReturnsAsync(4900m);
        _beneficiaryRepo.Setup(r => r.GetByOwnerIdAsync(10)).ReturnsAsync(
            new[] { new Beneficiary { OwnerId = 10, BeneficiaryAccountNumber = "ACC0000000002", Status = BeneficiaryStatus.Approved } });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.TransferAsync(10, new TransferRequest(1, 2, 200m, null, null), null));
    }

    [Fact]
    public async Task Transfer_UnapprovedBeneficiary_Throws()
    {
        var from = MakeAccount(1, 10, 5000m);
        var to   = MakeAccount(2, 20, 0m);
        _accountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(from);
        _accountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(to);
        _transactionRepo.Setup(r => r.GetByReferenceAsync(It.IsAny<string>())).ReturnsAsync((Transaction?)null);
        _transactionRepo.Setup(r => r.GetDailyOutgoingTotalAsync(1, It.IsAny<DateTime>())).ReturnsAsync(0m);
        _beneficiaryRepo.Setup(r => r.GetByOwnerIdAsync(10)).ReturnsAsync(
            new[] { new Beneficiary { OwnerId = 10, BeneficiaryAccountNumber = "ACC0000000002", Status = BeneficiaryStatus.Pending } });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.TransferAsync(10, new TransferRequest(1, 2, 100m, null, null), null));
    }

    [Fact]
    public async Task Transfer_NotOwner_ThrowsUnauthorized()
    {
        var from = MakeAccount(1, 99, 5000m); // owned by 99, not 10
        var to   = MakeAccount(2, 20, 0m);
        _accountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(from);
        _accountRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(to);
        _transactionRepo.Setup(r => r.GetByReferenceAsync(It.IsAny<string>())).ReturnsAsync((Transaction?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.TransferAsync(10, new TransferRequest(1, 2, 100m, null, null), null));
    }
}
