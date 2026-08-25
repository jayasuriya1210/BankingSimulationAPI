namespace BankingSimulation.Data.Entities;

public class Account
{
    public long Id { get; set; }
    public string AccountNumber { get; set; } = null!;
    public long OwnerId { get; set; }
    public decimal Balance { get; set; }
    public decimal DailyTransferLimit { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public BankUser Owner { get; set; } = null!;
    public ICollection<Transaction> OutgoingTransactions { get; set; } = new List<Transaction>();
    public ICollection<Transaction> IncomingTransactions { get; set; } = new List<Transaction>();
}
