using BankingSimulation.Api.DTOs;
using BankingSimulation.Data.Repositories;

namespace BankingSimulation.Api.Services;

public class StatementService(
    IAccountRepository accountRepo,
    ITransactionRepository transactionRepo)
{
    public async Task<IEnumerable<TransactionResponse>> GenerateAsync(long userId, StatementRequest req, string role)
    {
        if (req.From > req.To)
            throw new ArgumentException("'From' date must be before 'To' date.");

        if ((req.To - req.From).TotalDays > 366)
            throw new ArgumentException("Date range cannot exceed 366 days.");

        var now = DateTime.UtcNow;
        if (req.From > now || req.To > now)
            throw new ArgumentException("Statement range cannot be in the future.", nameof(req));

        var account = await accountRepo.GetByIdAsync(req.AccountId)
            ?? throw new KeyNotFoundException("Account not found.");

        if (role == "Customer" && account.OwnerId != userId)
            throw new UnauthorizedAccessException("Access denied.");

        // If the requested date range is entirely before the account was created,
        // there can be no transactions or statement data to return. Treat this as a bad request.
        if (req.To < account.CreatedAtUtc)
            throw new ArgumentException("Requested statement range is before the account creation date. No statement available for the specified period.");

        var transactions = await transactionRepo.GetByAccountIdAsync(
            req.AccountId, req.From, req.To, null, null, null, 1, int.MaxValue);

        return transactions.Select(t => new TransactionResponse(
            t.Id, t.Reference, t.FromAccountId, t.ToAccountId,
            t.Amount, t.Type.ToString(), t.Status.ToString(),
            t.Description, t.CreatedAtUtc));
    }
}
