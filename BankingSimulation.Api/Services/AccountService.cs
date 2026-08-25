using BankingSimulation.Api.DTOs;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;

namespace BankingSimulation.Api.Services;

public class AccountService(
    IAccountRepository accountRepo,
    IUserRepository userRepo,
    AuditService auditService)
{
    public async Task<IEnumerable<AccountResponse>> GetMyAccountsAsync(long userId)
    {
        var accounts = await accountRepo.GetByOwnerIdAsync(userId);
        return accounts.Select(Map);
    }

    public async Task<AccountResponse> CreateAccountAsync(long actorUserId, CreateAccountRequest req, string? correlationId)
    {
        var owner = await userRepo.GetByIdAsync(req.OwnerId)
            ?? throw new KeyNotFoundException("User not found.");

        var account = new Account
        {
            // AccountRepository replaces this temporary value with a number
            // based on the database-generated account ID.
            AccountNumber      = Guid.NewGuid().ToString("N"),
            OwnerId            = req.OwnerId,
            Balance            = 0,
            DailyTransferLimit = req.DailyTransferLimit,
            Status             = AccountStatus.Active,
            CreatedAtUtc       = DateTime.UtcNow,
            Owner              = owner
        };

        await accountRepo.CreateAsync(account);
        await auditService.LogAsync(actorUserId, "CreateAccount", "Account", account.Id.ToString(), $"Created for user {req.OwnerId}", "Success", correlationId);
        return Map(account);
    }

    public async Task<AccountResponse> GetAccountAsync(long accountId, long requestingUserId, string role)
    {
        var account = await accountRepo.GetByIdAsync(accountId)
            ?? throw new KeyNotFoundException("Account not found.");

        if (role == "Customer" && account.OwnerId != requestingUserId)
            throw new UnauthorizedAccessException("Access denied.");

        return Map(account);
    }

    public async Task<IEnumerable<AccountResponse>> GetAllAccountsAsync()
    {
        var accounts = await accountRepo.GetAllAsync();
        return accounts.Select(Map);
    }

    public async Task<AccountResponse> FreezeAccountAsync(long accountId, long actorUserId, string? reason, string? correlationId)
    {
        var account = await accountRepo.GetByIdAsync(accountId)
            ?? throw new KeyNotFoundException("Account not found.");

        if (account.Status == AccountStatus.Frozen)
            throw new InvalidOperationException("Account is already frozen.");

        account.Status = AccountStatus.Frozen;
        await accountRepo.UpdateAsync(account);

        await auditService.LogAsync(actorUserId, "FreezeAccount", "Account", accountId.ToString(), reason, "Success", correlationId);
        return Map(account);
    }

    public async Task<AccountResponse> UnfreezeAccountAsync(long accountId, long actorUserId, string? correlationId)
    {
        var account = await accountRepo.GetByIdAsync(accountId)
            ?? throw new KeyNotFoundException("Account not found.");

        if (account.Status != AccountStatus.Frozen)
            throw new InvalidOperationException("Account is not frozen.");

        account.Status = AccountStatus.Active;
        await accountRepo.UpdateAsync(account);

        await auditService.LogAsync(actorUserId, "UnfreezeAccount", "Account", accountId.ToString(), null, "Success", correlationId);
        return Map(account);
    }

    private static AccountResponse Map(Account a) => new(
        a.Id, a.AccountNumber, a.OwnerId,
        a.Owner?.DisplayName ?? string.Empty,
        a.Balance, a.DailyTransferLimit,
        a.Status.ToString(), a.CreatedAtUtc);
}
