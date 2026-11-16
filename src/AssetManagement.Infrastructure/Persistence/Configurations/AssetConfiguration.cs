using AssetManagement.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

/// <summary>Maps the asset hierarchy with table-per-hierarchy (TPH) inheritance.</summary>
internal sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public const string Discriminator = "AssetType";

    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");
        builder.HasKey(a => a.Id);

        builder.Property<AssetType>(Discriminator).HasConversion<string>().HasMaxLength(32);
        builder.HasDiscriminator<AssetType>(Discriminator)
            .HasValue<LaserScanner>(AssetType.LaserScanner)
            .HasValue<Drone>(AssetType.Drone)
            .HasValue<LaserTracker>(AssetType.LaserTracker);

        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.SerialNumber).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Manufacturer).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Model).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Location).HasMaxLength(200);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(32);

        builder.Property<Guid>(AppDbContext.ConcurrencyStamp).IsConcurrencyToken();

        // Behaviour, not state.
        builder.Ignore(a => a.Type);
        builder.Ignore(a => a.MaintenancePolicy);
        builder.Ignore(a => a.MaxCheckoutDuration);

        builder.HasIndex(a => a.SerialNumber).IsUnique();
        builder.HasIndex(a => a.Status);
    }
}

internal sealed class LaserScannerConfiguration : IEntityTypeConfiguration<LaserScanner>
{
    public void Configure(EntityTypeBuilder<LaserScanner> builder) =>
        builder.Property(s => s.RangeMeters).HasColumnName("RangeMeters");
}

internal sealed class DroneConfiguration : IEntityTypeConfiguration<Drone>
{
    public void Configure(EntityTypeBuilder<Drone> builder) =>
        builder.Property(d => d.MaxFlightTimeMinutes).HasColumnName("MaxFlightTimeMinutes");
}

internal sealed class LaserTrackerConfiguration : IEntityTypeConfiguration<LaserTracker>
{
    public void Configure(EntityTypeBuilder<LaserTracker> builder) =>
        builder.Property(t => t.AccuracyMicrometers).HasColumnName("AccuracyMicrometers");
}
