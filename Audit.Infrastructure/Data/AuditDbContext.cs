using Audit.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Audit.Infrastructure.Data;

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options)
    : DbContext(options)
{
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Policy>(e =>
        {
            e.ToTable("Policies");
            e.HasKey(p => p.Id);
            e.Property(p => p.ClientId).HasMaxLength(100).IsRequired();
            e.Property(p => p.PackageId).HasMaxLength(100).IsRequired();
            e.Property(p => p.QuoteRef).HasMaxLength(6).IsRequired();
            e.Property(p => p.PolicyNumber).HasMaxLength(100).IsRequired();
            e.Property(p => p.Status).HasMaxLength(30).IsRequired();
            e.HasIndex(p => new { p.ClientId, p.PackageId });
            e.HasIndex(p => new { p.PolicyNumber });
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasKey(o => o.Id);
            e.Property(o => o.EventId).HasMaxLength(64).IsRequired();
            e.HasIndex(o => o.EventId).IsUnique();
            e.Property(o => o.EventType).HasMaxLength(200).IsRequired();
            e.Property(o => o.PayloadJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();
            e.Property(o => o.LastError).HasMaxLength(2000);
            e.HasIndex(o => o.CreatedAtUtc)
                .HasDatabaseName("IX_OutboxMessages_Pending")
                .HasFilter("[PublishedAtUtc] IS NULL");
        });
    }
}