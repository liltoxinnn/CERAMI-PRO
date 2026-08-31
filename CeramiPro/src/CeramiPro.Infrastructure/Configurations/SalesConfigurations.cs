using CeramiPro.Domain.Entities.CustomOrders;
using CeramiPro.Domain.Entities.Invoicing;
using CeramiPro.Domain.Entities.Payments;
using CeramiPro.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CeramiPro.Infrastructure.Configurations;

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

        builder.Ignore(c => c.RemainingAmount);

        builder.HasIndex(c => c.OrderNumber).IsUnique();
        builder.HasIndex(c => c.Deadline);
        builder.HasIndex(c => c.Status);

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

public class CustomOrderImageConfiguration : IEntityTypeConfiguration<CustomOrderImage>
{
    public void Configure(EntityTypeBuilder<CustomOrderImage> builder)
    {
        builder.ToTable("CustomOrderImages");

        builder.HasQueryFilter(i => !i.CustomOrder.IsDeleted);

        builder.Property(i => i.FilePath).HasMaxLength(400).IsRequired();
        builder.Property(i => i.Caption).HasMaxLength(200);

        builder.HasOne(i => i.CustomOrder)
            .WithMany(c => c.Images)
            .HasForeignKey(i => i.CustomOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CustomOrderNoteConfiguration : IEntityTypeConfiguration<CustomOrderNote>
{
    public void Configure(EntityTypeBuilder<CustomOrderNote> builder)
    {
        builder.ToTable("CustomOrderNotes");

        builder.HasQueryFilter(n => !n.CustomOrder.IsDeleted);

        builder.Property(n => n.Content).HasMaxLength(2000).IsRequired();

        builder.HasOne(n => n.CustomOrder)
            .WithMany(c => c.OrderNotes)
            .HasForeignKey(n => n.CustomOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.Property(s => s.SaleNumber).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.Subtotal).HasPrecision(18, 2);
        builder.Property(s => s.DiscountAmount).HasPrecision(18, 2);
        builder.Property(s => s.TaxAmount).HasPrecision(18, 2);
        builder.Property(s => s.TotalAmount).HasPrecision(18, 2);
        builder.Property(s => s.PaidAmount).HasPrecision(18, 2);
        builder.Property(s => s.TotalCost).HasPrecision(18, 2);

        builder.Ignore(s => s.RemainingAmount);

        builder.HasIndex(s => s.SaleNumber).IsUnique();
        builder.HasIndex(s => s.SaleDate);

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");

        // Une vente annulée ne doit plus alimenter les statistiques.
        builder.HasQueryFilter(i => !i.Sale.IsDeleted);

        builder.Property(i => i.Description).HasMaxLength(300).IsRequired();
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2);
        builder.Property(i => i.UnitCost).HasPrecision(18, 4);

        builder.HasOne(i => i.Sale)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany(p => p.SaleItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ProductVariant)
            .WithMany()
            .HasForeignKey(i => i.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

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

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.Property(p => p.PaymentNumber).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Reference).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.Amount).HasPrecision(18, 2);

        builder.HasIndex(p => p.PaymentNumber).IsUnique();
        builder.HasIndex(p => p.PaymentDate);

        builder.HasOne(p => p.Customer)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Sale)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.CustomOrder)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.CustomOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
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

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.Property(i => i.InvoiceNumber).HasMaxLength(30).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(2000);

        builder.Property(i => i.Subtotal).HasPrecision(18, 2);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.TaxRate).HasPrecision(5, 2);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2);

        builder.Ignore(i => i.RemainingAmount);

        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.IssueDate);

        builder.HasOne(i => i.Customer)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Sale)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.CustomOrder)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CustomOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems");

        builder.HasQueryFilter(i => !i.Invoice.IsDeleted);

        builder.Property(i => i.Description).HasMaxLength(300).IsRequired();
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2);

        builder.HasOne(i => i.Invoice)
            .WithMany(f => f.Items)
            .HasForeignKey(i => i.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
