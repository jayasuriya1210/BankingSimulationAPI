using BankingSimulation.Data.Entities;

namespace BankingSimulation.Data.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(long id);
    Task<Account?> GetByNumberAsync(string accountNumber);
    Task<IEnumerable<Account>> GetByOwnerIdAsync(long ownerId);
    Task<IEnumerable<Account>> GetAllAsync();
    Task<Account> CreateAsync(Account account);
    Task UpdateAsync(Account account);
}
