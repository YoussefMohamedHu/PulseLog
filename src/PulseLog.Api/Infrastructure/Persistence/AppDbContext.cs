using Microsoft.EntityFrameworkCore;
using PulseLog.Api.Domain.Entities;

namespace PulseLog.Api.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Incident> Incidents { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AuditEntry> AuditEntries { get; set; }
}
