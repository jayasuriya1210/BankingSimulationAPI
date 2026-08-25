using BankingSimulation.Api.Services;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;
using Moq;

namespace BankingSimulation.Tests;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        var auditService = new AuditService(_auditRepo.Object);
        _sut = new AccountService(_accountRepo.Object, _userRepo.Object, auditService);
    }

    [Fact]
    public async Task FreezeAccount_AlreadyFrozen_Throws()
    {
        var account = new Account { Id = 1, OwnerId = 5, Status = AccountStatus.Frozen,
            AccountNumber = "ACC001", Owner = new BankUser { DisplayName = "Test" } };
        _accountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.FreezeAccountAsync(1, 1, "test", null));
    }

    [Fact]
    public async Task UnfreezeAccount_NotFrozen_Throws()
    {
        var account = new Account { Id = 1, OwnerId = 5, Status = AccountStatus.Active,
            AccountNumber = "ACC001", Owner = new BankUser { DisplayName = "Test" } };
        _accountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UnfreezeAccountAsync(1, 1, null));
    }

    [Fact]
    public async Task GetAccount_CustomerAccessingOtherAccount_ThrowsUnauthorized()
    {
        var account = new Account { Id = 1, OwnerId = 99, Status = AccountStatus.Active,
            AccountNumber = "ACC001", Owner = new BankUser { DisplayName = "Other" } };
        _accountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetAccountAsync(1, 10, "Customer"));
    }

    [Fact]
    public async Task FreezeAccount_Active_UpdatesStatus()
    {
        var account = new Account { Id = 1, OwnerId = 5, Status = AccountStatus.Active,
            AccountNumber = "ACC001", Owner = new BankUser { DisplayName = "Test" } };
        _accountRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(account);
        _accountRepo.Setup(r => r.UpdateAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);
        _auditRepo.Setup(r => r.CreateAsync(It.IsAny<BankingSimulation.Data.Entities.AuditLog>())).Returns(Task.CompletedTask);

        var result = await _sut.FreezeAccountAsync(1, 1, "fraud", null);

        Assert.Equal("Frozen", result.Status);
    }
}
