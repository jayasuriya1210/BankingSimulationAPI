using BankingSimulation.Data.Entities;

namespace BankingSimulation.Data.Repositories;

public interface IBeneficiaryRepository
{
    Task<Beneficiary?> GetByIdAsync(long id);
    Task<IEnumerable<Beneficiary>> GetByOwnerIdAsync(long ownerId);
    Task<IEnumerable<Beneficiary>> GetPendingAsync();
    Task<bool> ExistsAsync(long ownerId, string accountNumber);
    Task<Beneficiary> CreateAsync(Beneficiary beneficiary);
    Task UpdateAsync(Beneficiary beneficiary);
}
