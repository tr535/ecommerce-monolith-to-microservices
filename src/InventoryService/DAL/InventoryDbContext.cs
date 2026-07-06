using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.DAL;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<InventoryItem> InventoryItems { get; set; }

    public DbSet<InventoryReservation> InventoryReservations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InventoryReservation>()
            .HasIndex(r => r.OrderId)
            .IsUnique();
    }
}