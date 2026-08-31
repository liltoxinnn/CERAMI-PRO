using CeramiPro.Application.Interfaces;
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
public class CeramiProDbContext : DbContext
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
