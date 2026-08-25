using BankingSimulation.Api.DTOs;
using BankingSimulation.Api.Services;
using BankingSimulation.Data.Database;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace BankingSimulation.Tests;

public class DepositServiceTests
{
    [Fact]
    public async Task DepositAsync_CreditsAccountAndCreatesTransaction()
    {
        await using var db = CreateDatabase();
        var auditRepo = new Mock<IAuditLogRepository>();
        auditRepo.Setup(r => r.CreateAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        var sut = CreateSut(db, auditRepo);

        var result = await sut.DepositAsync(1, 1,
            new DepositRequest(100m, "Initial funding", "deposit-success-1"), null);

        var account = await db.Accounts.SingleAsync(a => a.Id == 1);
        var transaction = await db.Transactions.SingleAsync(t => t.Reference == "deposit-success-1");

        Assert.Equal(10100m, account.Balance);
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(1, transaction.ToAccountId);
        Assert.Null(transaction.FromAccountId);
        Assert.Equal(100m, result.Amount);
        Assert.Equal("Deposit", result.Type);
    }

    [Fact]
    public async Task DepositAsync_SameIdempotencyKey_DoesNotCreditTwice()
    {
        await using var db = CreateDatabase();
        var auditRepo = new Mock<IAuditLogRepository>();
        auditRepo.Setup(r => r.CreateAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);
        var sut = CreateSut(db, auditRepo);
        var request = new DepositRequest(100m, "Initial funding", "deposit-idempotent-1");

        await sut.DepositAsync(1, 1, request, null);
        await sut.DepositAsync(1, 1, request, null);

        var account = await db.Accounts.SingleAsync(a => a.Id == 1);
        Assert.Equal(10100m, account.Balance);
        Assert.Equal(1, await db.Transactions.CountAsync(t => t.Reference == "deposit-idempotent-1"));
    }

    [Fact]
    public async Task DepositAsync_FrozenAccount_Throws()
    {
        await using var db = CreateDatabase();
        var account = await db.Accounts.SingleAsync(a => a.Id == 1);
        account.Status = AccountStatus.Frozen;
        await db.SaveChangesAsync();

        var sut = CreateSut(db, new Mock<IAuditLogRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DepositAsync(
            1, 1, new DepositRequest(100m, null, "deposit-frozen-1"), null));
    }

    [Fact]
    public async Task DepositAsync_MissingAccount_ThrowsNotFound()
    {
        await using var db = CreateDatabase();
        var sut = CreateSut(db, new Mock<IAuditLogRepository>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.DepositAsync(
            1, 999, new DepositRequest(100m, null, "deposit-missing-1"), null));
    }

    [Fact]
    public async Task DepositAsync_NonPositiveAmount_Throws()
    {
        await using var db = CreateDatabase();
        var sut = CreateSut(db, new Mock<IAuditLogRepository>());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.DepositAsync(
            1, 1, new DepositRequest(0m, null, "deposit-invalid-1"), null));
    }

    private static BankingDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new BankingDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static DepositService CreateSut(
        BankingDbContext db, Mock<IAuditLogRepository> auditRepo) =>
        new(new AccountRepository(db), new TransactionRepository(db),
            new AuditService(auditRepo.Object), db);
}
