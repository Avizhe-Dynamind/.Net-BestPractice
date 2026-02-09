using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;
using Equinox.Infra.CrossCutting.Identity.Models;

namespace Equinox.Infra.CrossCutting.Identity.Data
{
    public class EquinoxIdentityContext : IdentityDbContext
    {
        public EquinoxIdentityContext(DbContextOptions<EquinoxIdentityContext> options) : base(options) { }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.JwtId).IsRequired().HasMaxLength(200);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.DeviceId).HasMaxLength(200);

                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.JwtId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(500);
                entity.Property(e => e.DeviceId).HasMaxLength(200);
                entity.Property(e => e.DeviceName).HasMaxLength(200);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.Location).HasMaxLength(200);

                entity.HasIndex(e => e.SessionToken);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.IsActive });

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<SecurityAuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventDescription).HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.UserEmail).HasMaxLength(256);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => new { e.IpAddress, e.Timestamp });
            });
        }
    }

    public class EquinoxIdentityContextFactory : IDesignTimeDbContextFactory<EquinoxIdentityContext>
    {
        public EquinoxIdentityContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build();

            var builder = new DbContextOptionsBuilder<EquinoxIdentityContext>();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (environment == "Development")
            {
                builder.UseSqlite(connectionString);
            }
            else
            {
                builder.UseSqlServer(connectionString);
            }                

            return new EquinoxIdentityContext(builder.Options);
        }
    }
}