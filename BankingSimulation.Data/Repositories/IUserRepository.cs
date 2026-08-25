using BankingSimulation.Data.Entities;

namespace BankingSimulation.Data.Repositories;

public interface IUserRepository
{
    Task<BankUser?> GetByIdAsync(long id);
    Task<BankUser?> GetByEmailAsync(string email);
    Task<BankUser?> GetByKeycloakSubjectAsync(string subject);
    Task<BankUser> CreateAsync(BankUser user);
    Task UpdateAsync(BankUser user);
}
