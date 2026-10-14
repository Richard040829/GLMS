using GLMS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Client>().HasMany(c => c.Contracts).WithOne(c => c.Client)
            .HasForeignKey(c => c.ClientId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Contract>().HasMany(c => c.ServiceRequests).WithOne(s => s.Contract)
            .HasForeignKey(s => s.ContractId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Contract>().Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<ServiceRequest>().Property(s => s.CostInUSD).HasPrecision(18, 2);
        b.Entity<ServiceRequest>().Property(s => s.CostInZAR).HasPrecision(18, 2);

        // Seed data (fixed values so migrations are deterministic)
        b.Entity<Client>().HasData(
            new Client { Id = 1, Name = "Acme Freight", ContactDetails = "ops@acme.example | +27 11 555 0100", Region = "Africa" },
            new Client { Id = 2, Name = "Nordic Cargo AS", ContactDetails = "info@nordic.example | +47 22 55 00 11", Region = "Europe" });

        b.Entity<Contract>().HasData(
            new Contract { Id = 1, ClientId = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2027, 12, 31), Status = ContractStatus.Active, ServiceLevel = "Gold" },
            new Contract { Id = 2, ClientId = 1, StartDate = new DateTime(2023, 1, 1), EndDate = new DateTime(2024, 12, 31), Status = ContractStatus.Expired, ServiceLevel = "Silver" },
            new Contract { Id = 3, ClientId = 2, StartDate = new DateTime(2026, 3, 1), EndDate = new DateTime(2028, 2, 28), Status = ContractStatus.OnHold, ServiceLevel = "Platinum" },
            new Contract { Id = 4, ClientId = 2, StartDate = new DateTime(2026, 9, 1), EndDate = new DateTime(2027, 8, 31), Status = ContractStatus.Draft, ServiceLevel = "Bronze" });

        b.Entity<ServiceRequest>().HasData(
            new ServiceRequest { Id = 1, ContractId = 1, Description = "Container shipment Durban to Rotterdam", CostInUSD = 1000m, CostInZAR = 18500m, Status = "Pending" });
    }
}
