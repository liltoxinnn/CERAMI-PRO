using CeramiPro.Domain.Entities.Audit;
using CeramiPro.Domain.Entities.Catalog;
using CeramiPro.Domain.Entities.CustomOrders;
using CeramiPro.Domain.Entities.Customers;
using CeramiPro.Domain.Entities.Expenses;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Inventory;
using CeramiPro.Domain.Entities.Payments;
using CeramiPro.Domain.Entities.Sales;
using CeramiPro.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Accès aux données exposé aux services métier. L'implémentation concrète
/// (Entity Framework Core / PostgreSQL) reste dans la couche Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    // ------------------------------------------------------------- Comptes
    DbSet<User> Users { get; }

    // ------------------------------------------------------------- Clients
    DbSet<Customer> Customers { get; }
    DbSet<CustomerNote> CustomerNotes { get; }

    // ------------------------------------------------------------ Produits
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductImage> ProductImages { get; }

    // ----------------------------------------------------------- Commandes
    DbSet<CustomOrder> CustomOrders { get; }
    DbSet<CustomOrderImage> CustomOrderImages { get; }
    DbSet<CustomOrderNote> CustomOrderNotes { get; }

    // -------------------------------------------------- Ventes et règlements
    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }

    // --------------------------------------------------------------- Stock
    DbSet<InventoryTransaction> InventoryTransactions { get; }

    // ------------------------------------------------------------ Dépenses
    DbSet<ExpenseCategory> ExpenseCategories { get; }
    DbSet<Expense> Expenses { get; }

    // ------------------------------------------------- Réglages et journal
    DbSet<BusinessSettings> BusinessSettings { get; }
    DbSet<SystemSetting> SystemSettings { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
}
