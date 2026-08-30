using CeramicWorkshop.Domain.Entities.Audit;
using CeramicWorkshop.Domain.Entities.Catalog;
using CeramicWorkshop.Domain.Entities.CustomOrders;
using CeramicWorkshop.Domain.Entities.Customers;
using CeramicWorkshop.Domain.Entities.Decoration;
using CeramicWorkshop.Domain.Entities.Expenses;
using CeramicWorkshop.Domain.Entities.Firing;
using CeramicWorkshop.Domain.Entities.Identity;
using CeramicWorkshop.Domain.Entities.Inventory;
using CeramicWorkshop.Domain.Entities.Invoicing;
using CeramicWorkshop.Domain.Entities.Materials;
using CeramicWorkshop.Domain.Entities.Notifications;
using CeramicWorkshop.Domain.Entities.Payments;
using CeramicWorkshop.Domain.Entities.Production;
using CeramicWorkshop.Domain.Entities.Purchasing;
using CeramicWorkshop.Domain.Entities.Quality;
using CeramicWorkshop.Domain.Entities.Recipes;
using CeramicWorkshop.Domain.Entities.Sales;
using CeramicWorkshop.Domain.Entities.Settings;
using CeramicWorkshop.Domain.Entities.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Interfaces;

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
}
