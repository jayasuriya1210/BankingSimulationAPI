using BankingSimulation.Api.DTOs;
using BankingSimulation.Data.Database;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;

namespace BankingSimulation.Api.Services;

public class TransferService(
    IAccountRepository accountRepo,
    IBeneficiaryRepository beneficiaryRepo,
    ITransactionRepository transactionRepo,
    AuditService auditService,
    BankingDbContext db)
{
    public async Task<TransactionResponse> TransferAsync(long userId, TransferRequest req, string? correlationId)
    {
        // Idempotency check
        if (!string.IsNullOrWhiteSpace(req.IdempotencyKey))
        {
            var existing = await transactionRepo.GetByReferenceAsync(req.IdempotencyKey);
            if (existing is not null) return Map(existing);
        }

        var fromAccount = await accountRepo.GetByIdAsync(req.FromAccountId)
            ?? throw new KeyNotFoundException("Source account not found.");

        var toAccount = await accountRepo.GetByIdAsync(req.ToAccountId)
            ?? throw new KeyNotFoundException("Destination account not found.");

        if (fromAccount.OwnerId != userId)
            throw new UnauthorizedAccessException("You do not own the source account.");

        if (fromAccount.Status == AccountStatus.Frozen)
            throw new InvalidOperationException("Source account is frozen.");

        if (toAccount.Status == AccountStatus.Frozen)
            throw new InvalidOperationException("Destination account is frozen.");

        if (fromAccount.Balance < req.Amount)
            throw new InvalidOperationException("Insufficient balance.");

        var todayTotal = await transactionRepo.GetDailyOutgoingTotalAsync(fromAccount.Id, DateTime.UtcNow);
        if (todayTotal + req.Amount > fromAccount.DailyTransferLimit)
            throw new InvalidOperationException($"Daily transfer limit of {fromAccount.DailyTransferLimit:F2} exceeded.");

        // Beneficiary check — only for transfers to a different owner
        if (fromAccount.OwnerId != toAccount.OwnerId)
        {
            var beneficiary = (await beneficiaryRepo.GetByOwnerIdAsync(userId))
                .FirstOrDefault(b => b.BeneficiaryAccountNumber == toAccount.AccountNumber);

            if (beneficiary is null)
                throw new InvalidOperationException("Recipient must be an approved beneficiary.");

            if (beneficiary.Status != BeneficiaryStatus.Approved)
                throw new InvalidOperationException("Beneficiary is not yet approved.");
        }

        var reference = req.IdempotencyKey ?? $"TXN{DateTime.UtcNow:yyyyMMddHHmmssfff}{Guid.NewGuid():N}"[..50];

        await using var dbTx = await db.Database.BeginTransactionAsync();
        try
        {
            fromAccount.Balance -= req.Amount;
            toAccount.Balance   += req.Amount;

            await accountRepo.UpdateAsync(fromAccount);
            await accountRepo.UpdateAsync(toAccount);

            var transaction = new Transaction
            {
                Reference         = reference,
                FromAccountId     = fromAccount.Id,
                ToAccountId       = toAccount.Id,
                Amount            = req.Amount,
                Type              = TransactionType.Transfer,
                Status            = TransactionStatus.Completed,
                InitiatedByUserId = userId,
                Description       = req.Description,
                CreatedAtUtc      = DateTime.UtcNow
            };

            await transactionRepo.CreateAsync(transaction);
            await dbTx.CommitAsync();

            await auditService.LogAsync(userId, "Transfer", "Transaction", transaction.Id.ToString(),
                $"{fromAccount.AccountNumber} -> {toAccount.AccountNumber} : {req.Amount}", "Success", correlationId);

            return Map(transaction);
        }
        catch
        {
            await dbTx.RollbackAsync();
            await auditService.LogAsync(userId, "Transfer", "Transaction", null,
                $"Failed {req.FromAccountId} -> {req.ToAccountId} : {req.Amount}", "Failure", correlationId);
            throw;
        }
    }

    private static TransactionResponse Map(Transaction t) => new(
        t.Id, t.Reference, t.FromAccountId, t.ToAccountId,
        t.Amount, t.Type.ToString(), t.Status.ToString(),
        t.Description, t.CreatedAtUtc);
}
