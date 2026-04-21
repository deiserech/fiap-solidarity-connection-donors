using Microsoft.EntityFrameworkCore;
using SolidarityConnection.Domain.Entities;

namespace SolidarityConnection.Infrastructure.Data
{
        public class AppDbContext : DbContext
        {
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
            public DbSet<User> Users { get; set; }
            public DbSet<Campaign> Campaigns { get; set; }
            public DbSet<ProcessedDonation> ProcessedDonations { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
