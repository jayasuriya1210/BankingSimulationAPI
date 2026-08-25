namespace BankingSimulation.Data.Entities;

public class Beneficiary
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public string BeneficiaryAccountNumber { get; set; } = null!;
    public string BeneficiaryName { get; set; } = null!;
    public BeneficiaryStatus Status { get; set; } = BeneficiaryStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAtUtc { get; set; }
    public long? ReviewedByUserId { get; set; }

    public BankUser Owner { get; set; } = null!;
}
