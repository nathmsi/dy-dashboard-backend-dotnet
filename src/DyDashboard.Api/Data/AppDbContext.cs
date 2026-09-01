using DyDashboard.Api.Features.Campaigns;
using Microsoft.EntityFrameworkCore;

namespace DyDashboard.Api.Data;

/// <summary>
/// EF Core context — the single unit of work over the SQLite database. The
/// schema (columns, constraints, indexes) is declared here and materialised by
/// EF Core migrations, mirroring the hand-rolled migrations of the Node backend.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Campaign> Campaigns => Set<Campaign>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.ToTable("campaigns");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.Name).HasColumnName("name").IsRequired();
            entity.Property(c => c.Status).HasColumnName("status").IsRequired();
            entity.Property(c => c.Channel).HasColumnName("channel").IsRequired();
            entity.Property(c => c.ConversionRate).HasColumnName("conversionRate");
            entity.Property(c => c.Visitors).HasColumnName("visitors");
            entity.Property(c => c.StartDate).HasColumnName("startDate").IsRequired();
            entity.Property(c => c.CreatedAt).HasColumnName("createdAt").IsRequired();
            entity.Property(c => c.UpdatedAt).HasColumnName("updatedAt").IsRequired();

            // status ∈ { active, paused, ended } — enforced at the DB level too.
            entity.ToTable(t => t.HasCheckConstraint(
                "ck_campaigns_status", "status IN ('active', 'paused', 'ended')"));

            entity.HasIndex(c => c.Status).HasDatabaseName("idx_campaigns_status");
            entity.HasIndex(c => c.StartDate).HasDatabaseName("idx_campaigns_startDate");
        });
    }
}
