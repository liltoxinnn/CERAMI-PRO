using CeramicWorkshop.Domain.Entities.Materials;
using CeramicWorkshop.Domain.Entities.Purchasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CeramicWorkshop.Infrastructure.Configurations;

public class MaterialCategoryConfiguration : IEntityTypeConfiguration<MaterialCategory>
{
    public void Configure(EntityTypeBuilder<MaterialCategory> builder)
    {
        builder.ToTable("MaterialCategories");

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");

        builder.Property(u => u.Code).HasMaxLength(20).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(60).IsRequired();
        builder.Property(u => u.ConversionFactor).HasPrecision(18, 6);

        builder.HasIndex(u => u.Code).IsUnique();
    }
}

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materials");

        builder.Property(m => m.Reference).HasMaxLength(40).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(150).IsRequired();
        builder.Property(m => m.Location).HasMaxLength(100);
        builder.Property(m => m.Description).HasMaxLength(1000);
        builder.Property(m => m.ImagePath).HasMaxLength(400);

        builder.Property(m => m.CurrentQuantity).HasPrecision(18, 3);
        builder.Property(m => m.MinimumStock).HasPrecision(18, 3);
        builder.Property(m => m.MaximumStock).HasPrecision(18, 3);
        builder.Property(m => m.AverageCost).HasPrecision(18, 4);
        builder.Property(m => m.LastPurchasePrice).HasPrecision(18, 4);

        builder.HasIndex(m => m.Reference).IsUnique();
        builder.HasIndex(m => m.Name);

        builder.HasOne(m => m.MaterialCategory)
            .WithMany(c => c.Materials)
            .HasForeignKey(m => m.MaterialCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Unit)
            .WithMany(u => u.Materials)
            .HasForeignKey(m => m.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Supplier)
            .WithMany(s => s.Materials)
            .HasForeignKey(m => m.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class MaterialBatchConfiguration : IEntityTypeConfiguration<MaterialBatch>
{
    public void Configure(EntityTypeBuilder<MaterialBatch> builder)
    {
        builder.ToTable("MaterialBatches");

        builder.Property(b => b.BatchNumber).HasMaxLength(40).IsRequired();
        builder.Property(b => b.Location).HasMaxLength(100);
        builder.Property(b => b.Notes).HasMaxLength(1000);

        builder.Property(b => b.Quantity).HasPrecision(18, 3);
        builder.Property(b => b.RemainingQuantity).HasPrecision(18, 3);
        builder.Property(b => b.UnitCost).HasPrecision(18, 4);

        builder.HasIndex(b => b.BatchNumber).IsUnique();

        builder.HasOne(b => b.Material)
            .WithMany(m => m.Batches)
            .HasForeignKey(b => b.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.PurchaseItem)
            .WithMany(i => i.Batches)
            .HasForeignKey(b => b.PurchaseItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchases");

        builder.Property(p => p.PurchaseNumber).HasMaxLength(30).IsRequired();
        builder.Property(p => p.InvoiceReference).HasMaxLength(60);
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.Property(p => p.Subtotal).HasPrecision(18, 2);
        builder.Property(p => p.DiscountAmount).HasPrecision(18, 2);
        builder.Property(p => p.ShippingCost).HasPrecision(18, 2);
        builder.Property(p => p.TotalAmount).HasPrecision(18, 2);
        builder.Property(p => p.PaidAmount).HasPrecision(18, 2);

        // Montant calculé : il n'est pas stocké en base.
        builder.Ignore(p => p.RemainingAmount);

        builder.HasIndex(p => p.PurchaseNumber).IsUnique();
        builder.HasIndex(p => p.PurchaseDate);

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.Purchases)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("PurchaseItems");

        // Les lignes suivent l'achat : elles disparaissent avec lui des listes et des rapports.
        builder.HasQueryFilter(i => !i.Purchase.IsDeleted);

        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.ReceivedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 4);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2);

        builder.HasOne(i => i.Purchase)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Material)
            .WithMany(m => m.PurchaseItems)
            .HasForeignKey(i => i.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Unit)
            .WithMany()
            .HasForeignKey(i => i.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
