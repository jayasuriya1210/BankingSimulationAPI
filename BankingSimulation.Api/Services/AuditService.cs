using BankingSimulation.Data.Entities;
using BankingSimulation.Data.Repositories;

namespace BankingSimulation.Api.Services;

public class AuditService(IAuditLogRepository repo)
{
    public Task LogAsync(long? actorUserId, string action, string entityName,
        string? entityId = null, string? details = null,
        string result = "Success", string? correlationId = null) =>
        repo.CreateAsync(new AuditLog
        {
            ActorUserId   = actorUserId,
            Action        = action,
            EntityName    = entityName,
            EntityId      = entityId,
            Details       = details,
            Result        = result,
            CorrelationId = correlationId,
            CreatedAtUtc  = DateTime.UtcNow
        });
}
