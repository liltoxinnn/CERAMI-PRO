using CeramiPro.Domain.Entities.Catalog;
using CeramiPro.Domain.Entities.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CeramiPro.Infrastructure.Configurations;

/// <summary>Catégories de produits.</summary>
public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}

/// <summary>Produits du catalogue.</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.Property(p => p.Reference).HasMaxLength(40).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.MaterialDescription).HasMaxLength(150);
        builder.Property(p => p.Color).HasMaxLength(80);
        builder.Property(p => p.Finish).HasMaxLength(80);

        builder.Property(p => p.Width).HasPrecision(10, 2);
        builder.Property(p => p.Height).HasPrecision(10, 2);
        builder.Property(p => p.Depth).HasPrecision(10, 2);
        builder.Property(p => p.Weight).HasPrecision(10, 3);

        builder.Property(p => p.ProductionCost).HasPrecision(18, 2);
        builder.Property(p => p.SellingPrice).HasPrecision(18, 2);
        builder.Property(p => p.CurrentStock).HasPrecision(18, 3);
        builder.Property(p => p.MinimumStock).HasPrecision(18, 3);

        builder.HasIndex(p => p.Reference).IsUnique();
        builder.HasIndex(p => p.Name);

        builder.HasOne(p => p.ProductCategory)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.ProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Photographies d'un produit.</summary>
public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.Property(i => i.FilePath).HasMaxLength(400).IsRequired();
        builder.Property(i => i.Caption).HasMaxLength(200);

        builder.HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Clients de l'atelier.</summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.Property(c => c.CustomerNumber).HasMaxLength(30).IsRequired();
        builder.Property(c => c.FullName).HasMaxLength(150).IsRequired();
        builder.Property(c => c.PhoneNumber).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.Address).HasMaxLength(300);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.HasIndex(c => c.CustomerNumber).IsUnique();
        builder.HasIndex(c => c.FullName);
        builder.HasIndex(c => c.PhoneNumber);
    }
}

/// <summary>Suivi écrit des échanges avec un client.</summary>
public class CustomerNoteConfiguration : IEntityTypeConfiguration<CustomerNote>
{
    public void Configure(EntityTypeBuilder<CustomerNote> builder)
    {
        builder.ToTable("CustomerNotes");

        builder.Property(n => n.Content).HasMaxLength(2000).IsRequired();

        builder.HasOne(n => n.Customer)
            .WithMany(c => c.CustomerNotes)
            .HasForeignKey(n => n.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
