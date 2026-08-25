using BankingSimulation.Data.Database;
using BankingSimulation.Data.Entities;

namespace BankingSimulation.Data.Repositories;

public class AuditLogRepository(BankingDbContext db) : IAuditLogRepository
{
    public async Task CreateAsync(AuditLog log)
    {
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync();
    }
}
