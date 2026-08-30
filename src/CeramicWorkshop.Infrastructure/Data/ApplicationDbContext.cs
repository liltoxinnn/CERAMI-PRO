using System.Linq.Expressions;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
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

namespace CeramicWorkshop.Infrastructure.Data;

/// <summary>
/// Contexte Entity Framework Core de la base « CeramicWorkshopDB » (PostgreSQL).
/// Il remplit automatiquement les informations de traçabilité et applique
/// la suppression logique des documents financiers (règle métier n°15).
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _utilisateurCourant;
    private readonly IDateTimeService? _horloge;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService utilisateurCourant,
        IDateTimeService horloge)
        : base(options)
    {
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();

    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialBatch> MaterialBatches => Set<MaterialBatch>();

    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductRecipe> ProductRecipes => Set<ProductRecipe>();
    public DbSet<ProductRecipeItem> ProductRecipeItems => Set<ProductRecipeItem>();

    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<ProductionMaterial> ProductionMaterials => Set<ProductionMaterial>();
    public DbSet<ProductionStageHistory> ProductionStageHistory => Set<ProductionStageHistory>();

    public DbSet<Kiln> Kilns => Set<Kiln>();
    public DbSet<FiringBatch> FiringBatches => Set<FiringBatch>();
    public DbSet<FiringBatchItem> FiringBatchItems => Set<FiringBatchItem>();

    public DbSet<DecorationType> DecorationTypes => Set<DecorationType>();
    public DbSet<DecorationOrder> DecorationOrders => Set<DecorationOrder>();
    public DbSet<DecorationImage> DecorationImages => Set<DecorationImage>();

    public DbSet<QualityCheck> QualityChecks => Set<QualityCheck>();
    public DbSet<QualityIssue> QualityIssues => Set<QualityIssue>();

    public DbSet<CustomOrder> CustomOrders => Set<CustomOrder>();
    public DbSet<CustomOrderImage> CustomOrderImages => Set<CustomOrderImage>();
    public DbSet<CustomOrderNote> CustomOrderNotes => Set<CustomOrderNote>();

    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        AppliquerFiltreSuppressionLogique(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Précision par défaut : suffisante pour les montants comme pour les quantités.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);

        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AppliquerTracabilite();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        AppliquerTracabilite();
        return base.SaveChanges();
    }

    /// <summary>
    /// Remplit les dates et l'utilisateur responsable, et transforme les suppressions
    /// des documents financiers en suppression logique.
    /// </summary>
    private void AppliquerTracabilite()
    {
        var maintenant = _horloge?.UtcNow ?? DateTime.UtcNow;
        var utilisateurId = _utilisateurCourant?.UserId;

        foreach (var entree in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entree.State)
            {
                case EntityState.Added:
                    entree.Entity.CreatedAt = maintenant;
                    entree.Entity.CreatedByUserId ??= utilisateurId;
                    break;
                case EntityState.Modified:
                    entree.Entity.UpdatedAt = maintenant;
                    entree.Entity.UpdatedByUserId = utilisateurId;
                    break;
            }
        }

        foreach (var entree in ChangeTracker.Entries<ISoftDeletable>().Where(e => e.State == EntityState.Deleted))
        {
            entree.State = EntityState.Modified;
            entree.Entity.IsDeleted = true;
            entree.Entity.DeletedAt = maintenant;
            entree.Entity.DeletedByUserId = utilisateurId;
        }
    }

    /// <summary>Masque automatiquement les enregistrements supprimés logiquement.</summary>
    private static void AppliquerFiltreSuppressionLogique(ModelBuilder modelBuilder)
    {
        foreach (var type in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(ISoftDeletable).IsAssignableFrom(t.ClrType)))
        {
            var parametre = Expression.Parameter(type.ClrType, "e");
            var propriete = Expression.Property(parametre, nameof(ISoftDeletable.IsDeleted));
            var filtre = Expression.Lambda(Expression.Not(propriete), parametre);

            modelBuilder.Entity(type.ClrType).HasQueryFilter(filtre);
        }
    }
}
