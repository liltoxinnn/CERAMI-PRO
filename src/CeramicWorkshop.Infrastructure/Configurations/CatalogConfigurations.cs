using CeramicWorkshop.Domain.Entities.Catalog;
using CeramicWorkshop.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CeramicWorkshop.Infrastructure.Configurations;

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
        builder.Property(p => p.Barcode).HasMaxLength(60);
        builder.Property(p => p.QrCode).HasMaxLength(120);

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
        builder.HasIndex(p => p.Barcode);

        builder.HasOne(p => p.ProductCategory)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.ProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

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

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.Property(v => v.Reference).HasMaxLength(40).IsRequired();
        builder.Property(v => v.Name).HasMaxLength(150).IsRequired();
        builder.Property(v => v.Color).HasMaxLength(80);
        builder.Property(v => v.Size).HasMaxLength(80);
        builder.Property(v => v.Barcode).HasMaxLength(60);

        builder.Property(v => v.PriceAdjustment).HasPrecision(18, 2);
        builder.Property(v => v.CurrentStock).HasPrecision(18, 3);
        builder.Property(v => v.MinimumStock).HasPrecision(18, 3);

        builder.HasIndex(v => v.Reference).IsUnique();

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductRecipeConfiguration : IEntityTypeConfiguration<ProductRecipe>
{
    public void Configure(EntityTypeBuilder<ProductRecipe> builder)
    {
        builder.ToTable("ProductRecipes");

        builder.Property(r => r.Name).HasMaxLength(150).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(2000);

        builder.Property(r => r.YieldQuantity).HasPrecision(18, 3);
        builder.Property(r => r.LaborCost).HasPrecision(18, 2);
        builder.Property(r => r.FiringCost).HasPrecision(18, 2);
        builder.Property(r => r.DecorationCost).HasPrecision(18, 2);
        builder.Property(r => r.PackagingCost).HasPrecision(18, 2);
        builder.Property(r => r.OtherCost).HasPrecision(18, 2);

        builder.HasIndex(r => new { r.ProductId, r.Version }).IsUnique();

        builder.HasOne(r => r.Product)
            .WithMany(p => p.Recipes)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductRecipeItemConfiguration : IEntityTypeConfiguration<ProductRecipeItem>
{
    public void Configure(EntityTypeBuilder<ProductRecipeItem> builder)
    {
        builder.ToTable("ProductRecipeItems");

        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.Property(i => i.Quantity).HasPrecision(18, 4);
        builder.Property(i => i.WastePercentage).HasPrecision(5, 2);

        builder.HasOne(i => i.ProductRecipe)
            .WithMany(r => r.Items)
            .HasForeignKey(i => i.ProductRecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Material)
            .WithMany(m => m.RecipeItems)
            .HasForeignKey(i => i.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Unit)
            .WithMany()
            .HasForeignKey(i => i.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
