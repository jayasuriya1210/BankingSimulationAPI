using BankingSimulation.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingSimulation.Data.Database;

public class BankingDbContext(DbContextOptions<BankingDbContext> options) : DbContext(options)
{
    public DbSet<BankUser> Users => Set<BankUser>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BankUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.KeycloakSubject).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.KeycloakSubject).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.AccountNumber).IsUnique();
            e.Property(x => x.AccountNumber).HasMaxLength(34).IsRequired();
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.Property(x => x.DailyTransferLimit).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Owner).WithMany(x => x.Accounts)
                .HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Beneficiary>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OwnerId, x.BeneficiaryAccountNumber }).IsUnique();
            e.Property(x => x.BeneficiaryAccountNumber).HasMaxLength(34).IsRequired();
            e.Property(x => x.BeneficiaryName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Owner).WithMany(x => x.Beneficiaries)
                .HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Reference).IsUnique();
            e.HasIndex(x => x.CreatedAtUtc);
            e.Property(x => x.Reference).HasMaxLength(50).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.FromAccount).WithMany(x => x.OutgoingTransactions)
                .HasForeignKey(x => x.FromAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToAccount).WithMany(x => x.IncomingTransactions)
                .HasForeignKey(x => x.ToAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CreatedAtUtc);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Result).HasMaxLength(20).IsRequired();
            e.Property(x => x.CorrelationId).HasMaxLength(50);
            e.HasOne(x => x.Actor).WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        });

        // Seed users — KeycloakSubject must be updated to match real Keycloak sub values
        modelBuilder.Entity<BankUser>().HasData(
            new BankUser { Id = 1, KeycloakSubject = "335bf9c2-af59-45d0-a96a-9d67361b5ae8", Email = "admin@bank.com",    DisplayName = "System Admin",  Role = "Admin",    IsActive = true, CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new BankUser { Id = 2, KeycloakSubject = "c0c8c433-231a-4464-adae-61517e8765db", Email = "staff@bank.com",    DisplayName = "Bank Staff",    Role = "Staff",    IsActive = true, CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new BankUser { Id = 3, KeycloakSubject = "a1122118-1905-4180-a2f9-870d5c0477ae", Email = "customer@bank.com", DisplayName = "John Customer", Role = "Customer", IsActive = true, CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<Account>().HasData(
            new Account { Id = 1, AccountNumber = "ACC0000000001", OwnerId = 3, Balance = 10000.00m, DailyTransferLimit = 5000.00m, Status = AccountStatus.Active, CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Account { Id = 2, AccountNumber = "ACC0000000002", OwnerId = 3, Balance = 2500.00m,  DailyTransferLimit = 5000.00m, Status = AccountStatus.Active, CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
