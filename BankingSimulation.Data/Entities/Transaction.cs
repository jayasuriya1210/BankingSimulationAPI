namespace BankingSimulation.Data.Entities;

public class Transaction
{
    public long Id { get; set; }
    public string Reference { get; set; } = null!;
    public long? FromAccountId { get; set; }
    public long? ToAccountId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public long InitiatedByUserId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Account? FromAccount { get; set; }
    public Account? ToAccount { get; set; }
}
