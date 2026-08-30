using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Referentiels;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Catalog;
using CeramicWorkshop.Domain.Entities.Decoration;
using CeramicWorkshop.Domain.Entities.Expenses;
using CeramicWorkshop.Domain.Entities.Materials;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Gère d'une manière uniforme les listes simples de l'atelier : catégories de
/// matières, de produits, de dépenses et types de décoration. Une entrée déjà
/// utilisée ne peut pas être supprimée ; elle se désactive.
/// </summary>
public class ReferentielService : IReferentielService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;

    public ReferentielService(IApplicationDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<IReadOnlyList<ElementReferentielDto>> ListerAsync(
        TypeReferentiel type, bool inclureInactifs = true, CancellationToken cancellationToken = default)
        => type switch
        {
            TypeReferentiel.CategorieMatiere => await _context.MaterialCategories
                .AsNoTracking()
                .Where(c => inclureInactifs || c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new ElementReferentielDto(c.Id, c.Name, c.Description, c.IsActive, false,
                    c.Materials.Count))
                .ToListAsync(cancellationToken),

            TypeReferentiel.CategorieProduit => await _context.ProductCategories
                .AsNoTracking()
                .Where(c => inclureInactifs || c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new ElementReferentielDto(c.Id, c.Name, c.Description, c.IsActive, false,
                    c.Products.Count))
                .ToListAsync(cancellationToken),

            TypeReferentiel.CategorieDepense => await _context.ExpenseCategories
                .AsNoTracking()
                .Where(c => inclureInactifs || c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new ElementReferentielDto(c.Id, c.Name, c.Description, c.IsActive, c.IsSystem,
                    c.Expenses.Count))
                .ToListAsync(cancellationToken),

            _ => await _context.DecorationTypes
                .AsNoTracking()
                .Where(t => inclureInactifs || t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new ElementReferentielDto(t.Id, t.Name, t.Description, t.IsActive, false,
                    t.DecorationOrders.Count))
                .ToListAsync(cancellationToken)
        };

    public async Task<ElementReferentielDto> CreerAsync(
        TypeReferentiel type, ElementReferentielRequete requete, CancellationToken cancellationToken = default)
    {
        var nom = requete.Nom.Trim();
        await VerifierNomLibreAsync(type, nom, null, cancellationToken);

        switch (type)
        {
            case TypeReferentiel.CategorieMatiere:
                _context.MaterialCategories.Add(new MaterialCategory
                {
                    Name = nom, Description = requete.Description?.Trim(), IsActive = requete.Actif
                });
                break;
            case TypeReferentiel.CategorieProduit:
                _context.ProductCategories.Add(new ProductCategory
                {
                    Name = nom, Description = requete.Description?.Trim(), IsActive = requete.Actif
                });
                break;
            case TypeReferentiel.CategorieDepense:
                _context.ExpenseCategories.Add(new ExpenseCategory
                {
                    Name = nom, Description = requete.Description?.Trim(), IsActive = requete.Actif
                });
                break;
            default:
                _context.DecorationTypes.Add(new DecorationType
                {
                    Name = nom, Description = requete.Description?.Trim(), IsActive = requete.Actif
                });
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Creation, type.ToString(), null,
            $"Ajout de « {nom} » dans « {type.Libelle()} ».", null, cancellationToken);

        var liste = await ListerAsync(type, true, cancellationToken);
        return liste.First(e => e.Nom == nom);
    }

    public async Task<ElementReferentielDto> ModifierAsync(
        TypeReferentiel type, int id, ElementReferentielRequete requete, CancellationToken cancellationToken = default)
    {
        var nom = requete.Nom.Trim();
        await VerifierNomLibreAsync(type, nom, id, cancellationToken);

        switch (type)
        {
            case TypeReferentiel.CategorieMatiere:
            {
                var element = await Trouver(_context.MaterialCategories, id, type, cancellationToken);
                element.Name = nom;
                element.Description = requete.Description?.Trim();
                element.IsActive = requete.Actif;
                break;
            }
            case TypeReferentiel.CategorieProduit:
            {
                var element = await Trouver(_context.ProductCategories, id, type, cancellationToken);
                element.Name = nom;
                element.Description = requete.Description?.Trim();
                element.IsActive = requete.Actif;
                break;
            }
            case TypeReferentiel.CategorieDepense:
            {
                var element = await Trouver(_context.ExpenseCategories, id, type, cancellationToken);
                element.Name = nom;
                element.Description = requete.Description?.Trim();
                element.IsActive = requete.Actif;
                break;
            }
            default:
            {
                var element = await Trouver(_context.DecorationTypes, id, type, cancellationToken);
                element.Name = nom;
                element.Description = requete.Description?.Trim();
                element.IsActive = requete.Actif;
                break;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, type.ToString(), id.ToString(),
            $"Modification de « {nom} » dans « {type.Libelle()} ».", null, cancellationToken);

        return (await ListerAsync(type, true, cancellationToken)).First(e => e.Id == id);
    }

    public async Task SupprimerAsync(TypeReferentiel type, int id, CancellationToken cancellationToken = default)
    {
        var element = (await ListerAsync(type, true, cancellationToken)).FirstOrDefault(e => e.Id == id)
                      ?? throw NotFoundException.Pour(type.Libelle(), id);

        if (element.NombreUtilisations > 0)
        {
            throw new BusinessRuleException(
                $"« {element.Nom} » est utilisé par {element.NombreUtilisations} enregistrement(s). " +
                "Désactivez-le au lieu de le supprimer, afin de conserver l'historique.");
        }

        if (element.Systeme)
        {
            throw new BusinessRuleException(
                $"« {element.Nom} » est livré avec le logiciel : il peut être désactivé mais pas supprimé.");
        }

        switch (type)
        {
            case TypeReferentiel.CategorieMatiere:
                _context.MaterialCategories.Remove(
                    await Trouver(_context.MaterialCategories, id, type, cancellationToken));
                break;
            case TypeReferentiel.CategorieProduit:
                _context.ProductCategories.Remove(
                    await Trouver(_context.ProductCategories, id, type, cancellationToken));
                break;
            case TypeReferentiel.CategorieDepense:
                _context.ExpenseCategories.Remove(
                    await Trouver(_context.ExpenseCategories, id, type, cancellationToken));
                break;
            default:
                _context.DecorationTypes.Remove(
                    await Trouver(_context.DecorationTypes, id, type, cancellationToken));
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Suppression, type.ToString(), id.ToString(),
            $"Suppression de « {element.Nom} » dans « {type.Libelle()} ».", null, cancellationToken);
    }

    public async Task<IReadOnlyList<ModeReglementDto>> ListerModesReglementAsync(
        CancellationToken cancellationToken = default)
        => await _context.PaymentMethods
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new ModeReglementDto(m.Id, m.Code, m.Name, m.RequiresReference, m.IsActive))
            .ToListAsync(cancellationToken);

    private static async Task<TEntite> Trouver<TEntite>(
        DbSet<TEntite> ensemble, int id, TypeReferentiel type, CancellationToken cancellationToken)
        where TEntite : Domain.Common.BaseEntity
        => await ensemble.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
           ?? throw NotFoundException.Pour(type.Libelle(), id);

    private async Task VerifierNomLibreAsync(
        TypeReferentiel type, string nom, int? idExclu, CancellationToken cancellationToken)
    {
        var existe = (await ListerAsync(type, true, cancellationToken))
            .Any(e => e.Id != idExclu && string.Equals(e.Nom, nom, StringComparison.OrdinalIgnoreCase));

        if (existe)
        {
            throw new BusinessRuleException($"« {nom} » existe déjà dans cette liste.");
        }
    }
}
