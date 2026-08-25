using BankingSimulation.Api.DTOs;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;

namespace BankingSimulation.Api.Services;

public class TransactionService(
    ITransactionRepository transactionRepo,
    IAccountRepository accountRepo)
{
    public async Task<IEnumerable<TransactionResponse>> GetMyTransactionsAsync(long userId, long accountId, TransactionFilterRequest filter)
    {
        var account = await accountRepo.GetByIdAsync(accountId)
            ?? throw new KeyNotFoundException("Account not found.");

        if (account.OwnerId != userId)
            throw new UnauthorizedAccessException("Access denied.");

        TransactionType? type = filter.Type is not null
            ? Enum.Parse<TransactionType>(filter.Type, true) : null;

        var txns = await transactionRepo.GetByAccountIdAsync(
            accountId, filter.From, filter.To, type,
            filter.MinAmount, filter.MaxAmount, filter.Page, filter.PageSize);

        return txns.Select(Map);
    }

    public async Task<IEnumerable<TransactionResponse>> GetAllTransactionsAsync(TransactionFilterRequest filter)
    {
        var txns = await transactionRepo.GetAllAsync(filter.From, filter.To, filter.Page, filter.PageSize);
        return txns.Select(Map);
    }

    private static TransactionResponse Map(Transaction t) => new(
        t.Id, t.Reference, t.FromAccountId, t.ToAccountId,
        t.Amount, t.Type.ToString(), t.Status.ToString(),
        t.Description, t.CreatedAtUtc);
}
