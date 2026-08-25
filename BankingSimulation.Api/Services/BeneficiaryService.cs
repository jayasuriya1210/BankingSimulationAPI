using BankingSimulation.Api.DTOs;
using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;

namespace BankingSimulation.Api.Services;

public class BeneficiaryService(
    IBeneficiaryRepository beneficiaryRepo,
    IAccountRepository accountRepo,
    AuditService auditService)
{
    public async Task<BeneficiaryResponse> AddAsync(long ownerId, AddBeneficiaryRequest req, string? correlationId)
    {
        if (await beneficiaryRepo.ExistsAsync(ownerId, req.AccountNumber))
            throw new InvalidOperationException("Beneficiary with this account number already exists.");

        var account = await accountRepo.GetByNumberAsync(req.AccountNumber)
            ?? throw new KeyNotFoundException("Beneficiary account number not found in the system.");

        var beneficiary = new Beneficiary
        {
            OwnerId                  = ownerId,
            BeneficiaryAccountNumber = req.AccountNumber,
            BeneficiaryName          = req.Name,
            Status                   = BeneficiaryStatus.Pending,
            CreatedAtUtc             = DateTime.UtcNow
        };

        await beneficiaryRepo.CreateAsync(beneficiary);
        await auditService.LogAsync(ownerId, "AddBeneficiary", "Beneficiary", beneficiary.Id.ToString(), req.AccountNumber, "Success", correlationId);
        return Map(beneficiary);
    }

    public async Task<IEnumerable<BeneficiaryResponse>> GetMyBeneficiariesAsync(long ownerId)
    {
        var list = await beneficiaryRepo.GetByOwnerIdAsync(ownerId);
        return list.Select(Map);
    }

    public async Task<IEnumerable<BeneficiaryResponse>> GetPendingAsync()
    {
        var list = await beneficiaryRepo.GetPendingAsync();
        return list.Select(Map);
    }

    public async Task<BeneficiaryResponse> ReviewAsync(long beneficiaryId, long reviewerUserId, ReviewBeneficiaryRequest req, string? correlationId)
    {
        var beneficiary = await beneficiaryRepo.GetByIdAsync(beneficiaryId)
            ?? throw new KeyNotFoundException("Beneficiary not found.");

        if (beneficiary.Status != BeneficiaryStatus.Pending)
            throw new InvalidOperationException("Beneficiary is not in pending state.");

        beneficiary.Status           = req.Decision.Equals("Approve", StringComparison.OrdinalIgnoreCase)
            ? BeneficiaryStatus.Approved : BeneficiaryStatus.Rejected;
        beneficiary.ReviewedAtUtc    = DateTime.UtcNow;
        beneficiary.ReviewedByUserId = reviewerUserId;

        await beneficiaryRepo.UpdateAsync(beneficiary);

        var action = beneficiary.Status == BeneficiaryStatus.Approved ? "ApproveBeneficiary" : "RejectBeneficiary";
        await auditService.LogAsync(reviewerUserId, action, "Beneficiary", beneficiaryId.ToString(), req.Reason, "Success", correlationId);
        return Map(beneficiary);
    }

    private static BeneficiaryResponse Map(Beneficiary b) => new(
        b.Id, b.BeneficiaryAccountNumber, b.BeneficiaryName,
        b.Status.ToString(), b.CreatedAtUtc, b.ReviewedAtUtc);
}
