using BankingSimulation.Data.Database;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BankingSimulation.Tests;

public class AccountRepositoryTests
{
    [Fact]
    public async Task CreateAsync_UsesGloballyUniqueNumberFromGeneratedId()
    {
        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new BankingDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var account = new Account
        {
            AccountNumber = "placeholder",
            OwnerId = 3,
            DailyTransferLimit = 5000m
        };

        var created = await new AccountRepository(db).CreateAsync(account);

        Assert.Equal("ACC0000000003", created.AccountNumber);
        Assert.Equal("ACC0000000003", await db.Accounts
            .Where(a => a.Id == created.Id)
            .Select(a => a.AccountNumber)
            .SingleAsync());
    }
}
