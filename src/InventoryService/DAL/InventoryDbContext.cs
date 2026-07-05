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
}