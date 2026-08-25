using BankingSimulation.Data.Entities;

namespace BankingSimulation.Data.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByReferenceAsync(string reference);
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(long accountId, DateTime? from, DateTime? to, TransactionType? type, decimal? minAmount, decimal? maxAmount, int page, int pageSize);
    Task<IEnumerable<Transaction>> GetAllAsync(DateTime? from, DateTime? to, int page, int pageSize);
    Task<decimal> GetDailyOutgoingTotalAsync(long fromAccountId, DateTime date);
    Task<Transaction> CreateAsync(Transaction transaction);
}
