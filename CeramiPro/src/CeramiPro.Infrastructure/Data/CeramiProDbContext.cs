using CeramiPro.Application.Common;
using CeramiPro.Application.Interfaces;
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

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<CustomOrder> CustomOrders => Set<CustomOrder>();
    public DbSet<CustomOrderImage> CustomOrderImages => Set<CustomOrderImage>();
    public DbSet<CustomOrderNote> CustomOrderNotes => Set<CustomOrderNote>();

    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();

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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AppliquerTracabilite();

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException erreur)
        {
            throw ConflitDeModification(erreur);
        }
    }

    public override int SaveChanges()
    {
        AppliquerTracabilite();

        try
        {
            return base.SaveChanges();
        }
        catch (DbUpdateConcurrencyException erreur)
        {
            throw ConflitDeModification(erreur);
        }
    }

    /// <summary>
    /// Un autre poste a modifié la même fiche entre la lecture et
    /// l'enregistrement : rien n'est écrit, et l'utilisateur recommence sur
    /// des chiffres à jour plutôt que d'écraser ceux de son collègue.
    /// </summary>
    private RegleMetierException ConflitDeModification(DbUpdateConcurrencyException erreur)
    {
        ChangeTracker.Clear();

        return new RegleMetierException(
            "Ces données viennent d'être modifiées sur un autre poste. " +
            "Rien n'a été enregistré : actualisez l'écran, vérifiez les montants et les " +
            "quantités, puis recommencez.", erreur);
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
