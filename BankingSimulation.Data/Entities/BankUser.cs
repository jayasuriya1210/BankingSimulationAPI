namespace BankingSimulation.Data.Entities;

public class BankUser
{
    public long Id { get; set; }
    public string KeycloakSubject { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Role { get; set; } = "Customer";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
