using CeramicWorkshop.Domain.Entities.Decoration;
using CeramicWorkshop.Domain.Entities.Firing;
using CeramicWorkshop.Domain.Entities.Production;
using CeramicWorkshop.Domain.Entities.Quality;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CeramicWorkshop.Infrastructure.Configurations;

public class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.ToTable("ProductionOrders");

        builder.Property(o => o.ProductionNumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.Notes).HasMaxLength(2000);
        builder.Property(o => o.OverrideReason).HasMaxLength(500);

        builder.Property(o => o.PlannedQuantity).HasPrecision(18, 3);
        builder.Property(o => o.CompletedQuantity).HasPrecision(18, 3);
        builder.Property(o => o.DamagedQuantity).HasPrecision(18, 3);
        builder.Property(o => o.EstimatedMaterialCost).HasPrecision(18, 2);
        builder.Property(o => o.ActualMaterialCost).HasPrecision(18, 2);
        builder.Property(o => o.LaborCost).HasPrecision(18, 2);
        builder.Property(o => o.FiringCost).HasPrecision(18, 2);
        builder.Property(o => o.DecorationCost).HasPrecision(18, 2);
        builder.Property(o => o.PackagingCost).HasPrecision(18, 2);
        builder.Property(o => o.OtherCost).HasPrecision(18, 2);

        // Coûts calculés à la volée.
        builder.Ignore(o => o.TotalCost);
        builder.Ignore(o => o.UnitCost);

        builder.HasIndex(o => o.ProductionNumber).IsUnique();
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.PlannedEndDate);

        builder.HasOne(o => o.Product)
            .WithMany(p => p.ProductionOrders)
            .HasForeignKey(o => o.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.ProductRecipe)
            .WithMany()
            .HasForeignKey(o => o.ProductRecipeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.CustomOrder)
            .WithMany(c => c.ProductionOrders)
            .HasForeignKey(o => o.CustomOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.AssignedUser)
            .WithMany(u => u.AssignedProductionOrders)
            .HasForeignKey(o => o.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProductionMaterialConfiguration : IEntityTypeConfiguration<ProductionMaterial>
{
    public void Configure(EntityTypeBuilder<ProductionMaterial> builder)
    {
        builder.ToTable("ProductionMaterials");

        builder.HasQueryFilter(m => !m.ProductionOrder.IsDeleted);

        builder.Property(m => m.Notes).HasMaxLength(500);
        builder.Property(m => m.PlannedQuantity).HasPrecision(18, 4);
        builder.Property(m => m.ConsumedQuantity).HasPrecision(18, 4);
        builder.Property(m => m.UnitCost).HasPrecision(18, 4);
        builder.Property(m => m.TotalCost).HasPrecision(18, 2);

        builder.HasOne(m => m.ProductionOrder)
            .WithMany(o => o.Materials)
            .HasForeignKey(m => m.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Material)
            .WithMany(m => m.ProductionMaterials)
            .HasForeignKey(m => m.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Unit)
            .WithMany()
            .HasForeignKey(m => m.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductionStageHistoryConfiguration : IEntityTypeConfiguration<ProductionStageHistory>
{
    public void Configure(EntityTypeBuilder<ProductionStageHistory> builder)
    {
        builder.ToTable("ProductionStageHistory");

        builder.HasQueryFilter(h => !h.ProductionOrder.IsDeleted);

        builder.Property(h => h.Notes).HasMaxLength(1000);
        builder.Property(h => h.AcceptedQuantity).HasPrecision(18, 3);
        builder.Property(h => h.DamagedQuantity).HasPrecision(18, 3);

        builder.HasIndex(h => new { h.ProductionOrderId, h.Stage });

        builder.HasOne(h => h.ProductionOrder)
            .WithMany(o => o.StageHistory)
            .HasForeignKey(h => h.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class KilnConfiguration : IEntityTypeConfiguration<Kiln>
{
    public void Configure(EntityTypeBuilder<Kiln> builder)
    {
        builder.ToTable("Kilns");

        builder.Property(k => k.Reference).HasMaxLength(30).IsRequired();
        builder.Property(k => k.Name).HasMaxLength(100).IsRequired();
        builder.Property(k => k.Location).HasMaxLength(100);
        builder.Property(k => k.Notes).HasMaxLength(1000);

        builder.Property(k => k.Capacity).HasPrecision(18, 2);
        builder.Property(k => k.MinTemperature).HasPrecision(8, 2);
        builder.Property(k => k.MaxTemperature).HasPrecision(8, 2);

        builder.HasIndex(k => k.Reference).IsUnique();
    }
}

public class FiringBatchConfiguration : IEntityTypeConfiguration<FiringBatch>
{
    public void Configure(EntityTypeBuilder<FiringBatch> builder)
    {
        builder.ToTable("FiringBatches");

        builder.Property(b => b.BatchNumber).HasMaxLength(30).IsRequired();
        builder.Property(b => b.Observations).HasMaxLength(2000);

        builder.Property(b => b.Temperature).HasPrecision(8, 2);
        builder.Property(b => b.EnergyCost).HasPrecision(18, 2);
        builder.Property(b => b.DamagedQuantity).HasPrecision(18, 3);

        // Durée déduite des heures de début et de fin.
        builder.Ignore(b => b.DurationHours);

        builder.HasIndex(b => b.BatchNumber).IsUnique();
        builder.HasIndex(b => b.StartTime);

        builder.HasOne(b => b.Kiln)
            .WithMany(k => k.FiringBatches)
            .HasForeignKey(b => b.KilnId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class FiringBatchItemConfiguration : IEntityTypeConfiguration<FiringBatchItem>
{
    public void Configure(EntityTypeBuilder<FiringBatchItem> builder)
    {
        builder.ToTable("FiringBatchItems");

        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.AcceptedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.DamagedQuantity).HasPrecision(18, 3);
        builder.Property(i => i.AllocatedEnergyCost).HasPrecision(18, 2);

        builder.HasOne(i => i.FiringBatch)
            .WithMany(b => b.Items)
            .HasForeignKey(i => i.FiringBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ProductionOrder)
            .WithMany(o => o.FiringBatchItems)
            .HasForeignKey(i => i.ProductionOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DecorationTypeConfiguration : IEntityTypeConfiguration<DecorationType>
{
    public void Configure(EntityTypeBuilder<DecorationType> builder)
    {
        builder.ToTable("DecorationTypes");

        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}

public class DecorationOrderConfiguration : IEntityTypeConfiguration<DecorationOrder>
{
    public void Configure(EntityTypeBuilder<DecorationOrder> builder)
    {
        builder.ToTable("DecorationOrders");

        builder.Property(d => d.Reference).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Colors).HasMaxLength(300);
        builder.Property(d => d.Glaze).HasMaxLength(150);
        builder.Property(d => d.Paint).HasMaxLength(150);
        builder.Property(d => d.MaterialsUsed).HasMaxLength(1000);
        builder.Property(d => d.Notes).HasMaxLength(2000);

        builder.Property(d => d.Quantity).HasPrecision(18, 3);
        builder.Property(d => d.GoldQuantity).HasPrecision(10, 3);
        builder.Property(d => d.SilverQuantity).HasPrecision(10, 3);
        builder.Property(d => d.Cost).HasPrecision(18, 2);

        builder.HasIndex(d => d.Reference).IsUnique();

        builder.HasOne(d => d.DecorationType)
            .WithMany(t => t.DecorationOrders)
            .HasForeignKey(d => d.DecorationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ProductionOrder)
            .WithMany(o => o.DecorationOrders)
            .HasForeignKey(d => d.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.CustomOrder)
            .WithMany()
            .HasForeignKey(d => d.CustomOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.AssignedUser)
            .WithMany()
            .HasForeignKey(d => d.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class DecorationImageConfiguration : IEntityTypeConfiguration<DecorationImage>
{
    public void Configure(EntityTypeBuilder<DecorationImage> builder)
    {
        builder.ToTable("DecorationImages");

        builder.Property(i => i.FilePath).HasMaxLength(400).IsRequired();
        builder.Property(i => i.Caption).HasMaxLength(200);

        builder.HasOne(i => i.DecorationOrder)
            .WithMany(d => d.Images)
            .HasForeignKey(i => i.DecorationOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class QualityCheckConfiguration : IEntityTypeConfiguration<QualityCheck>
{
    public void Configure(EntityTypeBuilder<QualityCheck> builder)
    {
        builder.ToTable("QualityChecks");

        builder.Property(q => q.Reference).HasMaxLength(30).IsRequired();
        builder.Property(q => q.Notes).HasMaxLength(2000);

        builder.Property(q => q.InspectedQuantity).HasPrecision(18, 3);
        builder.Property(q => q.AcceptedQuantity).HasPrecision(18, 3);
        builder.Property(q => q.RejectedQuantity).HasPrecision(18, 3);
        builder.Property(q => q.ReworkQuantity).HasPrecision(18, 3);

        builder.HasIndex(q => q.Reference).IsUnique();

        builder.HasOne(q => q.ProductionOrder)
            .WithMany(o => o.QualityChecks)
            .HasForeignKey(q => q.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.CustomOrder)
            .WithMany()
            .HasForeignKey(q => q.CustomOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.FiringBatch)
            .WithMany()
            .HasForeignKey(q => q.FiringBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.CheckedByUser)
            .WithMany()
            .HasForeignKey(q => q.CheckedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class QualityIssueConfiguration : IEntityTypeConfiguration<QualityIssue>
{
    public void Configure(EntityTypeBuilder<QualityIssue> builder)
    {
        builder.ToTable("QualityIssues");

        builder.Property(i => i.Description).HasMaxLength(1000).IsRequired();
        builder.Property(i => i.Solution).HasMaxLength(1000);
        builder.Property(i => i.Quantity).HasPrecision(18, 3);

        builder.HasOne(i => i.QualityCheck)
            .WithMany(q => q.Issues)
            .HasForeignKey(i => i.QualityCheckId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
