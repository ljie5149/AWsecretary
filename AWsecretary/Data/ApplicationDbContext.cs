using System;
using System.Threading;
using System.Threading.Tasks;
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

        // 在 SaveChanges 之前自動設定建立/修改時間
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            UpdateTimestamps();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var now = DateTime.Now;

            foreach (var entry in ChangeTracker.Entries<Member>())
            {
                if (entry.State == EntityState.Added)
                {
                    // 新增時同時設定建立與修改時間
                    entry.Entity.CreateDate = now;
                    entry.Entity.ModifyDate = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // 每次修改時更新 ModifyDate，並避免意外修改 CreateDate
                    entry.Entity.ModifyDate = now;
                    entry.Property(e => e.CreateDate).IsModified = false;
                }
            }
        }
    }
}