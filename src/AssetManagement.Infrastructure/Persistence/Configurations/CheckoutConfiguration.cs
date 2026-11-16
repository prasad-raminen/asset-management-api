using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Checkouts;
using AssetManagement.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class CheckoutConfiguration : IEntityTypeConfiguration<Checkout>
{
    public void Configure(EntityTypeBuilder<Checkout> builder)
    {
        builder.ToTable("Checkouts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Notes).HasMaxLength(500);
        builder.Ignore(c => c.IsActive);

        builder.HasOne<Asset>().WithMany().HasForeignKey(c => c.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);

        // Database-level guarantee: at most one open checkout per asset.
        // Bracket quoting is understood by both SQL Server and SQLite.
        builder.HasIndex(c => c.AssetId)
            .IsUnique()
            .HasFilter("[ReturnedAtUtc] IS NULL")
            .HasDatabaseName("IX_Checkouts_AssetId_Active");

        builder.HasIndex(c => c.DueAtUtc);
    }
}
