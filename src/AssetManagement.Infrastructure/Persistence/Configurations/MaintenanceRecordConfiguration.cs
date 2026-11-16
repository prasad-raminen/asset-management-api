using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using AssetManagement.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("MaintenanceRecords");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(m => m.Description).HasMaxLength(1000).IsRequired();
        builder.Property(m => m.ResolutionNotes).HasMaxLength(1000);
        builder.Property(m => m.Cost).HasPrecision(12, 2);
        builder.Ignore(m => m.IsOpen);

        builder.HasOne<Asset>().WithMany().HasForeignKey(m => m.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.TechnicianId).OnDelete(DeleteBehavior.Restrict);
    }
}
