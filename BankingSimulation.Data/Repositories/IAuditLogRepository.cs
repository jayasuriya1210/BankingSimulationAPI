using BankingSimulation.Data.Entities;

namespace BankingSimulation.Data.Repositories;

public interface IAuditLogRepository
{
    Task CreateAsync(AuditLog log);
}
