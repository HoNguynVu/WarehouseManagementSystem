using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infracstructure.Data
{
    public partial class IdentityDbContext : DbContext
    {
        public IdentityDbContext()
        {

        }

        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
        {
        }

        //Auth
        public virtual DbSet<Accounts> Accounts { get; set; }
        public virtual DbSet<Otps> Otps { get; set; }
        public virtual DbSet<RefreshTokens> RefreshTokens { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
               
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Accounts>(entity =>
            {
                entity.ToTable("Accounts");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
                entity.Property(e => e.Status).HasDefaultValue(true);
            });

            modelBuilder.Entity<Otps>(entity =>
                {
                    entity.ToTable("Otps");
                    entity.HasKey(e => e.Id);
                    entity.HasOne(e => e.Account)
                    .WithMany(a => a.Otps)
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity<RefreshTokens>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Account)
                .WithMany(a => a.RefreshTokens)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
