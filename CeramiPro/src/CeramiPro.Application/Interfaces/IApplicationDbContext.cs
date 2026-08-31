using CeramiPro.Domain.Entities.Audit;
using CeramiPro.Domain.Entities.Catalog;
using CeramiPro.Domain.Entities.CustomOrders;
using CeramiPro.Domain.Entities.Customers;
using CeramiPro.Domain.Entities.Decoration;
using CeramiPro.Domain.Entities.Expenses;
using CeramiPro.Domain.Entities.Firing;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Inventory;
using CeramiPro.Domain.Entities.Invoicing;
using CeramiPro.Domain.Entities.Materials;
using CeramiPro.Domain.Entities.Notifications;
using CeramiPro.Domain.Entities.Payments;
using CeramiPro.Domain.Entities.Production;
using CeramiPro.Domain.Entities.Purchasing;
using CeramiPro.Domain.Entities.Quality;
using CeramiPro.Domain.Entities.Recipes;
using CeramiPro.Domain.Entities.Sales;
using CeramiPro.Domain.Entities.Settings;
using CeramiPro.Domain.Entities.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Accès aux données exposé aux services métier. L'implémentation concrète
/// (Entity Framework Core / PostgreSQL) reste dans la couche Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }

    DbSet<Customer> Customers { get; }
    DbSet<CustomerNote> CustomerNotes { get; }

    DbSet<Supplier> Suppliers { get; }
    DbSet<SupplierPayment> SupplierPayments { get; }

    DbSet<MaterialCategory> MaterialCategories { get; }
    DbSet<Unit> Units { get; }
    DbSet<Material> Materials { get; }
    DbSet<MaterialBatch> MaterialBatches { get; }

    DbSet<Purchase> Purchases { get; }
    DbSet<PurchaseItem> PurchaseItems { get; }

    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductVariant> ProductVariants { get; }

    DbSet<ProductRecipe> ProductRecipes { get; }
    DbSet<ProductRecipeItem> ProductRecipeItems { get; }

    DbSet<ProductionOrder> ProductionOrders { get; }
    DbSet<ProductionMaterial> ProductionMaterials { get; }
    DbSet<ProductionStageHistory> ProductionStageHistory { get; }

    DbSet<Kiln> Kilns { get; }
    DbSet<FiringBatch> FiringBatches { get; }
    DbSet<FiringBatchItem> FiringBatchItems { get; }

    DbSet<DecorationType> DecorationTypes { get; }
    DbSet<DecorationOrder> DecorationOrders { get; }
    DbSet<DecorationImage> DecorationImages { get; }

    DbSet<QualityCheck> QualityChecks { get; }
    DbSet<QualityIssue> QualityIssues { get; }

    DbSet<CustomOrder> CustomOrders { get; }
    DbSet<CustomOrderImage> CustomOrderImages { get; }
    DbSet<CustomOrderNote> CustomOrderNotes { get; }

    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }

    DbSet<Payment> Payments { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }

    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }

    DbSet<InventoryTransaction> InventoryTransactions { get; }
    DbSet<StockAdjustment> StockAdjustments { get; }

    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<Expense> Expenses { get; }

    DbSet<Notification> Notifications { get; }
    DbSet<NotificationSetting> NotificationSettings { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<BusinessSettings> BusinessSettings { get; }
    DbSet<SystemSetting> SystemSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Oublie les modifications en attente. Sert lorsqu'un enregistrement a
    /// échoué et qu'il ne faut surtout pas le rejouer.
    /// </summary>
    void AnnulerModificationsEnAttente();
}
