using AssetTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetTracker.Infrastructure.Data;

public class AssetTrackerDbContext : DbContext
{
    public AssetTrackerDbContext(DbContextOptions<AssetTrackerDbContext> options) : base(options) { }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).HasColumnName("id");
            entity.Property(d => d.DeviceId).HasColumnName("device_id").HasMaxLength(64).IsRequired();
            entity.HasIndex(d => d.DeviceId).IsUnique();
            entity.Property(d => d.DisplayName).HasColumnName("display_name").HasMaxLength(128);
            entity.Property(d => d.ApiKeyHash).HasColumnName("api_key_hash").HasMaxLength(64).IsRequired();
            entity.Property(d => d.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).HasColumnName("id");
            entity.Property(l => l.DeviceFk).HasColumnName("device_fk").IsRequired();
            entity.Property(l => l.Timestamp).HasColumnName("timestamp").IsRequired();
            entity.Property(l => l.Latitude).HasColumnName("latitude").IsRequired();
            entity.Property(l => l.Longitude).HasColumnName("longitude").IsRequired();
            entity.Property(l => l.Altitude).HasColumnName("altitude");
            entity.Property(l => l.Speed).HasColumnName("speed");
            entity.Property(l => l.Satellites).HasColumnName("satellites");
            entity.Property(l => l.Hdop).HasColumnName("hdop");
            entity.Property(l => l.BatteryVoltage).HasColumnName("battery_voltage");
            entity.Property(l => l.IsStale).HasColumnName("is_stale").IsRequired();
            entity.Property(l => l.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.HasOne<Device>().WithMany().HasForeignKey(l => l.DeviceFk).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(l => new { l.DeviceFk, l.Timestamp }).HasDatabaseName("idx_locations_device_timestamp");
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasColumnName("id");
            entity.Property(a => a.Username).HasColumnName("username").HasMaxLength(64).IsRequired();
            entity.HasIndex(a => a.Username).IsUnique();
            entity.Property(a => a.PasswordHash).HasColumnName("password_hash").HasColumnType("varchar(60)").IsRequired();
            entity.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasData(new
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "$2a$11$w12C1tmcv4IC7YmfNIm9sOhwTrLehZMio3BmNDNKmrG/iDDu2RstC",
                CreatedAt = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc)
            });
        });
    }
}
