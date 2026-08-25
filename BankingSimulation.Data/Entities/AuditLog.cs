namespace BankingSimulation.Data.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public long? ActorUserId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityName { get; set; } = null!;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string Result { get; set; } = "Success";
    public string? CorrelationId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public BankUser? Actor { get; set; }
}
