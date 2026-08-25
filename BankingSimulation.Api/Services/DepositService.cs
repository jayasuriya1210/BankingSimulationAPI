using BankingSimulation.Api.DTOs;
using BankingSimulation.Data.Database;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;

namespace BankingSimulation.Api.Services;

public interface IDepositService
{
    Task<TransactionResponse> DepositAsync(
        long actorUserId, long accountId, DepositRequest req, string? correlationId);
}

public class DepositService(
    IAccountRepository accountRepo,
    ITransactionRepository transactionRepo,
    AuditService auditService,
    BankingDbContext db) : IDepositService
{
    public async Task<TransactionResponse> DepositAsync(
        long actorUserId, long accountId, DepositRequest req, string? correlationId)
    {
        if (req.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(req.Amount), "Deposit amount must be greater than zero.");

        if (!string.IsNullOrWhiteSpace(req.IdempotencyKey))
        {
            var existing = await transactionRepo.GetByReferenceAsync(req.IdempotencyKey);
            if (existing is not null) return Map(existing);
        }

        var account = await accountRepo.GetByIdAsync(accountId)
            ?? throw new KeyNotFoundException("Account not found.");

        if (account.Status == AccountStatus.Frozen)
            throw new InvalidOperationException("Account is frozen.");

        var reference = req.IdempotencyKey
            ?? $"DEP{DateTime.UtcNow:yyyyMMddHHmmssfff}{Guid.NewGuid():N}"[..50];

        await using var dbTx = await db.Database.BeginTransactionAsync();
        try
        {
            account.Balance += req.Amount;
            await accountRepo.UpdateAsync(account);

            var transaction = new Transaction
            {
                Reference         = reference,
                FromAccountId     = null,
                ToAccountId       = account.Id,
                Amount            = req.Amount,
                Type              = TransactionType.Deposit,
                Status             = TransactionStatus.Completed,
                InitiatedByUserId = actorUserId,
                Description       = req.Description,
                CreatedAtUtc      = DateTime.UtcNow
            };

            await transactionRepo.CreateAsync(transaction);
            await dbTx.CommitAsync();

            await auditService.LogAsync(actorUserId, "Deposit", "Transaction", transaction.Id.ToString(),
                $"Deposited {req.Amount} to {account.AccountNumber}", "Success", correlationId);

            return Map(transaction);
        }
        catch
        {
            await dbTx.RollbackAsync();
            await auditService.LogAsync(actorUserId, "Deposit", "Transaction", null,
                $"Failed deposit to account {accountId}: {req.Amount}", "Failure", correlationId);
            throw;
        }
    }

    private static TransactionResponse Map(Transaction t) => new(
        t.Id, t.Reference, t.FromAccountId, t.ToAccountId,
        t.Amount, t.Type.ToString(), t.Status.ToString(),
        t.Description, t.CreatedAtUtc);
}
