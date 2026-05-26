using Microsoft.EntityFrameworkCore;
using AWsecretary.Models;

namespace AWsecretary.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Member> Members { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Member>()
                .HasIndex(m => m.Sid)
                .IsUnique();

            modelBuilder.Entity<Member>()
                .HasIndex(m => m.Mid)
                .IsUnique();
        }
    }
}