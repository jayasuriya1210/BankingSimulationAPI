using BankingSimulation.Data.Database;
using BankingSimulation.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingSimulation.Data.Repositories;

public class TransactionRepository(BankingDbContext db) : ITransactionRepository
{
    public Task<Transaction?> GetByReferenceAsync(string reference) =>
        db.Transactions.FirstOrDefaultAsync(t => t.Reference == reference);

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(long accountId, DateTime? from, DateTime? to,
        TransactionType? type, decimal? minAmount, decimal? maxAmount, int page, int pageSize)
    {
        var q = db.Transactions.Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId);
        if (from.HasValue)      q = q.Where(t => t.CreatedAtUtc >= from.Value);
        if (to.HasValue)        q = q.Where(t => t.CreatedAtUtc <= to.Value);
        if (type.HasValue)      q = q.Where(t => t.Type == type.Value);
        if (minAmount.HasValue) q = q.Where(t => t.Amount >= minAmount.Value);
        if (maxAmount.HasValue) q = q.Where(t => t.Amount <= maxAmount.Value);
        return await q.OrderByDescending(t => t.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync(DateTime? from, DateTime? to, int page, int pageSize)
    {
        var q = db.Transactions.AsQueryable();
        if (from.HasValue) q = q.Where(t => t.CreatedAtUtc >= from.Value);
        if (to.HasValue)   q = q.Where(t => t.CreatedAtUtc <= to.Value);
        return await q.OrderByDescending(t => t.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public Task<decimal> GetDailyOutgoingTotalAsync(long fromAccountId, DateTime date)
    {
        var start = date.Date;
        var end   = start.AddDays(1);
        return db.Transactions
            .Where(t => t.FromAccountId == fromAccountId && t.CreatedAtUtc >= start && t.CreatedAtUtc < end && t.Status == TransactionStatus.Completed)
            .SumAsync(t => t.Amount);
    }

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return transaction;
    }
}
