using CeramiPro.Application.Interfaces;
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
using CeramiPro.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CeramiPro.Infrastructure.Data;

/// <summary>
/// Accès à la base « CeramiProDB ».
///
/// Trois comportements sont appliqués automatiquement à chaque enregistrement :
/// les dates et l'auteur des modifications sont renseignés, les suppressions
/// des pièces comptables deviennent des suppressions logiques, et les entités
/// ainsi masquées disparaissent des lectures.
/// </summary>
public class CeramiProDbContext : DbContext, IApplicationDbContext
{
    private readonly IUtilisateurCourant? _utilisateurCourant;
    private readonly IServiceDateHeure? _horloge;

    public CeramiProDbContext(
        DbContextOptions<CeramiProDbContext> options,
        IUtilisateurCourant? utilisateurCourant = null,
        IServiceDateHeure? horloge = null)
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

        // Les configurations de chaque table vivent dans des classes dédiées.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CeramiProDbContext).Assembly);

        AppliquerFiltreSuppressionLogique(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Montants et quantités : précision suffisante pour les coûts de revient
        // sans jamais introduire d'erreur d'arrondi.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);

        // PostgreSQL exige des dates en temps universel.
        configurationBuilder.Properties<DateTime>().HaveConversion<ConvertisseurUtc>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<ConvertisseurUtcNullable>();
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
    /// Oublie les modifications en attente : ce qui n'a pas pu être enregistré
    /// ne doit pas être rejoué au prochain enregistrement.
    /// </summary>
    public void AnnulerModificationsEnAttente() => ChangeTracker.Clear();

    /// <summary>
    /// Masque partout les lignes supprimées logiquement, pour que le reste du
    /// logiciel n'ait jamais à y penser.
    /// </summary>
    private static void AppliquerFiltreSuppressionLogique(ModelBuilder modelBuilder)
    {
        foreach (var type in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(type.ClrType))
            {
                continue;
            }

            var parametre = System.Linq.Expressions.Expression.Parameter(type.ClrType, "e");
            var propriete = System.Linq.Expressions.Expression.Property(
                parametre, nameof(ISoftDeletable.IsDeleted));
            var filtre = System.Linq.Expressions.Expression.Lambda(
                System.Linq.Expressions.Expression.Not(propriete), parametre);

            modelBuilder.Entity(type.ClrType).HasQueryFilter(filtre);
        }
    }

    /// <summary>
    /// Renseigne les dates et l'auteur, et convertit la suppression des pièces
    /// comptables en suppression logique.
    /// </summary>
    private void AppliquerTracabilite()
    {
        var maintenant = _horloge?.MaintenantUtc ?? DateTime.UtcNow;
        var utilisateur = _utilisateurCourant?.UtilisateurId;

        foreach (var entree in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entree.State)
            {
                case EntityState.Added:
                    entree.Entity.CreatedAt = maintenant;
                    entree.Entity.CreatedByUserId = utilisateur;
                    break;

                case EntityState.Modified:
                    entree.Entity.UpdatedAt = maintenant;
                    entree.Entity.UpdatedByUserId = utilisateur;
                    break;
            }
        }

        foreach (var entree in ChangeTracker.Entries<ISoftDeletable>()
                     .Where(e => e.State == EntityState.Deleted))
        {
            entree.State = EntityState.Modified;
            entree.Entity.IsDeleted = true;
            entree.Entity.DeletedAt = maintenant;
            entree.Entity.DeletedByUserId = utilisateur;
        }
    }
}
