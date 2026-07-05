using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.DAL;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<NotificationMessage> Notifications { get; set; }
}