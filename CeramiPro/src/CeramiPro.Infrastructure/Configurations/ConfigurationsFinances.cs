using CeramiPro.Domain.Entities.Expenses;
using CeramiPro.Domain.Entities.Inventory;
using CeramiPro.Domain.Entities.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CeramiPro.Infrastructure.Configurations;

/// <summary>Modes de règlement.</summary>
public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("PaymentMethods");

        builder.Property(m => m.Code).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(80).IsRequired();

        builder.HasIndex(m => m.Code).IsUnique();
    }
}

/// <summary>Encaissements clients.</summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.Property(p => p.PaymentNumber).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Reference).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        
        builder.HasIndex(p => p.PaymentNumber).IsUnique();
        builder.HasIndex(p => p.PaymentDate);

        builder.HasOne(p => p.Customer)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Sale)
            .WithMany(v => v.Payments)
            .HasForeignKey(p => p.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CustomOrder)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.CustomOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PaymentMethod)
            .WithMany(m => m.Payments)
            .HasForeignKey(p => p.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.SetNull);

    }
}

/// <summary>Catégories de dépenses.</summary>
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

/// <summary>Dépenses de l'atelier.</summary>
public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.Property(d => d.Reference).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Amount).HasPrecision(18, 2);
        builder.Property(d => d.Description).HasMaxLength(500).IsRequired();
        builder.Property(d => d.ReceiptPath).HasMaxLength(400);
        
        builder.HasIndex(d => d.Reference).IsUnique();
        builder.HasIndex(d => d.ExpenseDate);

        builder.HasOne(d => d.ExpenseCategory)
            .WithMany(c => c.Expenses)
            .HasForeignKey(d => d.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.PaymentMethod)
            .WithMany()
            .HasForeignKey(d => d.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.SetNull);

    }
}

/// <summary>Mouvements de stock.</summary>
public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions");

        builder.Property(m => m.TransactionType).HasConversion<int>();

        // Quantités et coût unitaire : précision par défaut (4 décimales).
        builder.Property(m => m.TotalCost).HasPrecision(18, 2);

        builder.Property(m => m.Reference).HasMaxLength(200);
        builder.Property(m => m.Notes).HasMaxLength(1000);

        builder.HasIndex(m => m.OccurredAt);
        builder.HasIndex(m => new { m.ProductId, m.OccurredAt });

        builder.HasOne(m => m.Product)
            .WithMany(p => p.InventoryTransactions)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Sale)
            .WithMany()
            .HasForeignKey(m => m.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.CustomOrder)
            .WithMany()
            .HasForeignKey(m => m.CustomOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ReversedTransaction)
            .WithMany()
            .HasForeignKey(m => m.ReversedTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
