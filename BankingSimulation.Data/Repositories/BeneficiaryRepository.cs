using BankingSimulation.Data.Database;
using BankingSimulation.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingSimulation.Data.Repositories;

public class BeneficiaryRepository(BankingDbContext db) : IBeneficiaryRepository
{
    public Task<Beneficiary?> GetByIdAsync(long id) =>
        db.Beneficiaries.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<IEnumerable<Beneficiary>> GetByOwnerIdAsync(long ownerId) =>
        await db.Beneficiaries.Where(b => b.OwnerId == ownerId).ToListAsync();

    public async Task<IEnumerable<Beneficiary>> GetPendingAsync() =>
        await db.Beneficiaries.Where(b => b.Status == BeneficiaryStatus.Pending).ToListAsync();

    public Task<bool> ExistsAsync(long ownerId, string accountNumber) =>
        db.Beneficiaries.AnyAsync(b => b.OwnerId == ownerId && b.BeneficiaryAccountNumber == accountNumber);

    public async Task<Beneficiary> CreateAsync(Beneficiary beneficiary)
    {
        db.Beneficiaries.Add(beneficiary);
        await db.SaveChangesAsync();
        return beneficiary;
    }

    public async Task UpdateAsync(Beneficiary beneficiary)
    {
        db.Beneficiaries.Update(beneficiary);
        await db.SaveChangesAsync();
    }
}
