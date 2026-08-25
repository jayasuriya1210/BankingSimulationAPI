using BankingSimulation.Data.Database;
using BankingSimulation.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingSimulation.Data.Repositories;

public class UserRepository(BankingDbContext db) : IUserRepository
{
    public Task<BankUser?> GetByIdAsync(long id) =>
        db.Users.FindAsync(id).AsTask();

    public Task<BankUser?> GetByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<BankUser?> GetByKeycloakSubjectAsync(string subject) =>
        db.Users.FirstOrDefaultAsync(u => u.KeycloakSubject == subject);

    public async Task<BankUser> CreateAsync(BankUser user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(BankUser user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }
}
