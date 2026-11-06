using CeramiPro.Domain.Entities.CustomOrders;
using CeramiPro.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CeramiPro.Infrastructure.Configurations;

/// <summary>Commandes personnalisées.</summary>
public class CustomOrderConfiguration : IEntityTypeConfiguration<CustomOrder>
{
    public void Configure(EntityTypeBuilder<CustomOrder> builder)
    {
        builder.ToTable("CustomOrders");

        builder.Property(c => c.OrderNumber).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(4000);
        builder.Property(c => c.Colors).HasMaxLength(300);
        builder.Property(c => c.Materials).HasMaxLength(500);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        
        builder.Property(c => c.Width).HasPrecision(10, 2);
        builder.Property(c => c.Height).HasPrecision(10, 2);
        builder.Property(c => c.Depth).HasPrecision(10, 2);

        builder.Property(c => c.Quantity).HasPrecision(18, 3);
        builder.Property(c => c.UnitPrice).HasPrecision(18, 2);
        builder.Property(c => c.DiscountAmount).HasPrecision(18, 2);
        builder.Property(c => c.TotalAmount).HasPrecision(18, 2);
        builder.Property(c => c.PaidAmount).HasPrecision(18, 2);

        // Le reste à payer est calculé : il ne se stocke pas, sinon il se
        // désynchronise des paiements.
        builder.Ignore(c => c.RemainingAmount);

        builder.Property(c => c.Status).HasConversion<int>();

        builder.HasIndex(c => c.OrderNumber).IsUnique();
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.Deadline);

        builder.HasOne(c => c.Customer)
            .WithMany(cl => cl.CustomOrders)
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.AssignedUser)
            .WithMany()
            .HasForeignKey(c => c.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

    }
}

/// <summary>Photos de référence et de fabrication d'une commande.</summary>
public class CustomOrderImageConfiguration : IEntityTypeConfiguration<CustomOrderImage>
{
    public void Configure(EntityTypeBuilder<CustomOrderImage> builder)
    {
        builder.ToTable("CustomOrderImages");

        builder.Property(i => i.FilePath).HasMaxLength(400).IsRequired();
        builder.Property(i => i.Caption).HasMaxLength(200);
        builder.Property(i => i.Kind).HasConversion<int>();

        builder.HasOne(i => i.CustomOrder)
            .WithMany(c => c.Images)
            .HasForeignKey(i => i.CustomOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Une commande supprimée emporte ses photos avec elle dans les lectures.
        builder.HasQueryFilter(i => !i.CustomOrder.IsDeleted);
    }
}

/// <summary>Suivi écrit d'une commande.</summary>
public class CustomOrderNoteConfiguration : IEntityTypeConfiguration<CustomOrderNote>
{
    public void Configure(EntityTypeBuilder<CustomOrderNote> builder)
    {
        builder.ToTable("CustomOrderNotes");

        builder.Property(n => n.Content).HasMaxLength(2000).IsRequired();

        builder.HasOne(n => n.CustomOrder)
            .WithMany(c => c.OrderNotes)
            .HasForeignKey(n => n.CustomOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(n => !n.CustomOrder.IsDeleted);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Ventes de produits finis.</summary>
public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.Property(v => v.SaleNumber).HasMaxLength(30).IsRequired();
        builder.Property(v => v.Notes).HasMaxLength(2000);
        
        builder.Property(v => v.Subtotal).HasPrecision(18, 2);
        builder.Property(v => v.DiscountAmount).HasPrecision(18, 2);
        builder.Property(v => v.TaxAmount).HasPrecision(18, 2);
        builder.Property(v => v.TotalAmount).HasPrecision(18, 2);
        builder.Property(v => v.PaidAmount).HasPrecision(18, 2);
        builder.Property(v => v.TotalCost).HasPrecision(18, 2);

        builder.Ignore(v => v.RemainingAmount);

        builder.Property(v => v.Status).HasConversion<int>();

        builder.HasIndex(v => v.SaleNumber).IsUnique();
        builder.HasIndex(v => v.SaleDate);
        builder.HasIndex(v => v.Status);

        builder.HasOne(v => v.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.SetNull);

    }
}

/// <summary>Lignes d'une vente.</summary>
public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");

        builder.Property(l => l.Description).HasMaxLength(300).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(18, 3);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Property(l => l.DiscountAmount).HasPrecision(18, 2);
        builder.Property(l => l.LineTotal).HasPrecision(18, 2);
        // Coût de revient : précision par défaut (4 décimales), pour ne pas arrondir les marges.

        builder.HasOne(l => l.Sale)
            .WithMany(v => v.Items)
            .HasForeignKey(l => l.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Product)
            .WithMany(p => p.SaleItems)
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Les lignes d'une vente supprimée ne doivent compter nulle part,
        // en particulier dans les rapports des produits les plus vendus.
        builder.HasQueryFilter(l => !l.Sale.IsDeleted);
    }
}
