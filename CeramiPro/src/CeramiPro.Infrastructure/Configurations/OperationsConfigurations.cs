using CeramiPro.Domain.Entities.Audit;
using CeramiPro.Domain.Entities.Expenses;
using CeramiPro.Domain.Entities.Inventory;
using CeramiPro.Domain.Entities.Notifications;
using CeramiPro.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CeramiPro.Infrastructure.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions");

        builder.Property(t => t.Reference).HasMaxLength(60);
        builder.Property(t => t.Notes).HasMaxLength(1000);

        builder.Property(t => t.Quantity).HasPrecision(18, 4);
        builder.Property(t => t.QuantityBefore).HasPrecision(18, 4);
        builder.Property(t => t.QuantityAfter).HasPrecision(18, 4);
        builder.Property(t => t.UnitCost).HasPrecision(18, 4);
        builder.Property(t => t.TotalCost).HasPrecision(18, 2);

        builder.HasIndex(t => t.OccurredAt);
        builder.HasIndex(t => new { t.ItemType, t.MaterialId });
        builder.HasIndex(t => new { t.ItemType, t.ProductId });

        builder.HasOne(t => t.Material)
            .WithMany(m => m.InventoryTransactions)
            .HasForeignKey(t => t.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Product)
            .WithMany(p => p.InventoryTransactions)
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ProductVariant)
            .WithMany()
            .HasForeignKey(t => t.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.MaterialBatch)
            .WithMany()
            .HasForeignKey(t => t.MaterialBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Purchase)
            .WithMany()
            .HasForeignKey(t => t.PurchaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Sale)
            .WithMany()
            .HasForeignKey(t => t.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ProductionOrder)
            .WithMany(o => o.InventoryTransactions)
            .HasForeignKey(t => t.ProductionOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.StockAdjustment)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.StockAdjustmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ReversedTransaction)
            .WithMany()
            .HasForeignKey(t => t.ReversedTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments");

        builder.Property(a => a.Reference).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(1000);

        builder.Property(a => a.QuantityBefore).HasPrecision(18, 4);
        builder.Property(a => a.CountedQuantity).HasPrecision(18, 4);
        builder.Property(a => a.Difference).HasPrecision(18, 4);

        builder.HasIndex(a => a.Reference).IsUnique();

        builder.HasOne(a => a.Material)
            .WithMany()
            .HasForeignKey(a => a.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Product)
            .WithMany()
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.Property(e => e.Reference).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.ReceiptPath).HasMaxLength(400);
        builder.Property(e => e.Amount).HasPrecision(18, 2);

        builder.HasIndex(e => e.Reference).IsUnique();
        builder.HasIndex(e => e.ExpenseDate);

        builder.HasOne(e => e.ExpenseCategory)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PaymentMethod)
            .WithMany()
            .HasForeignKey(e => e.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Link).HasMaxLength(300);
        builder.Property(n => n.EntityName).HasMaxLength(80);

        builder.HasIndex(n => new { n.IsRead, n.CreatedAt });

        // Une même fiche ne peut porter qu'une seule alerte d'un type donné.
        // PostgreSQL considère les valeurs nulles comme distinctes : les
        // messages d'information, qui ne visent aucune fiche, ne sont pas
        // contraints par cet index.
        builder.HasIndex(n => new { n.Type, n.EntityName, n.EntityId })
            .IsUnique()
            .HasDatabaseName("IX_Notifications_Type_Entite");

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationSettingConfiguration : IEntityTypeConfiguration<NotificationSetting>
{
    public void Configure(EntityTypeBuilder<NotificationSetting> builder)
    {
        builder.ToTable("NotificationSettings");

        builder.Property(s => s.ThresholdValue).HasPrecision(18, 3);

        builder.HasIndex(s => s.Type).IsUnique();

        builder.HasOne(s => s.NotifyRole)
            .WithMany()
            .HasForeignKey(s => s.NotifyRoleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.Property(a => a.UserName).HasMaxLength(60);
        builder.Property(a => a.EntityName).HasMaxLength(80).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(40);
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.Property(a => a.Workstation).HasMaxLength(60);
        builder.Property(a => a.Changes).HasColumnType("jsonb");

        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });

        builder.HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class BusinessSettingsConfiguration : IEntityTypeConfiguration<BusinessSettings>
{
    public void Configure(EntityTypeBuilder<BusinessSettings> builder)
    {
        builder.ToTable("BusinessSettings");

        builder.Property(s => s.WorkshopName).HasMaxLength(150).IsRequired();
        builder.Property(s => s.LegalName).HasMaxLength(150);
        builder.Property(s => s.Address).HasMaxLength(300);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.PhoneNumber).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.Website).HasMaxLength(150);
        builder.Property(s => s.LogoPath).HasMaxLength(400);
        builder.Property(s => s.TaxIdentificationNumber).HasMaxLength(40);
        builder.Property(s => s.TradeRegisterNumber).HasMaxLength(40);
        builder.Property(s => s.TaxArticleNumber).HasMaxLength(40);
        builder.Property(s => s.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(s => s.CurrencySymbol).HasMaxLength(10).IsRequired();
        builder.Property(s => s.Culture).HasMaxLength(10).IsRequired();
        builder.Property(s => s.TimeZoneId).HasMaxLength(60).IsRequired();
        builder.Property(s => s.DateFormat).HasMaxLength(20).IsRequired();
        builder.Property(s => s.DefaultTaxRate).HasPrecision(5, 2);
        builder.Property(s => s.InvoicePrefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.SalePrefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.PurchasePrefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.ProductionPrefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.CustomOrderPrefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.PaymentPrefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.FiringPrefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.InvoiceFooter).HasMaxLength(1000);
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");

        builder.Property(s => s.Key).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(1000);
        builder.Property(s => s.Category).HasMaxLength(60).IsRequired();
        builder.Property(s => s.ValueType).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);

        builder.HasIndex(s => s.Key).IsUnique();
    }
}
