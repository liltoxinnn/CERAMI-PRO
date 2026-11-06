using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Catalog;
using CeramiPro.Domain.Entities.Expenses;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Gère d'une manière uniforme les deux listes simples de l'atelier : les
/// catégories de produits et les catégories de dépenses.
///
/// Une catégorie déjà employée ne se supprime pas : elle se désactive. Sans
/// cela, un produit ou une dépense se retrouverait rattaché à une catégorie
/// qui n'existe plus, et les rapports cesseraient d'être justes.
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
    {
        var elements = type == TypeReferentiel.CategorieProduit
            ? await _context.ProductCategories
                .AsNoTracking()
                .Select(c => new ElementReferentielDto(
                    c.Id, c.Name, c.Description, c.IsActive, c.IsSystem, c.Products.Count))
                .ToListAsync(cancellationToken)
            : await _context.ExpenseCategories
                .AsNoTracking()
                .Select(c => new ElementReferentielDto(
                    c.Id, c.Name, c.Description, c.IsActive, c.IsSystem, c.Expenses.Count))
                .ToListAsync(cancellationToken);

        return elements
            .Where(e => inclureInactifs || e.Actif)
            .OrderBy(e => e.Nom, StringComparer.CurrentCulture)
            .ToList();
    }

    public async Task<ElementReferentielDto> CreerAsync(
        TypeReferentiel type, ElementReferentielRequete requete, CancellationToken cancellationToken = default)
    {
        var nom = requete.Nom.Trim();

        if (string.IsNullOrWhiteSpace(nom))
        {
            throw new RegleMetierException("Le nom de la catégorie est obligatoire.");
        }

        await VerifierNomLibreAsync(type, nom, null, cancellationToken);

        int identifiant;

        if (type == TypeReferentiel.CategorieProduit)
        {
            var categorie = new ProductCategory
            {
                Name = nom,
                Description = Nettoyer(requete.Description),
                IsActive = requete.Actif
            };

            _context.ProductCategories.Add(categorie);
            await _context.SaveChangesAsync(cancellationToken);
            identifiant = categorie.Id;
        }
        else
        {
            var categorie = new ExpenseCategory
            {
                Name = nom,
                Description = Nettoyer(requete.Description),
                IsActive = requete.Actif
            };

            _context.ExpenseCategories.Add(categorie);
            await _context.SaveChangesAsync(cancellationToken);
            identifiant = categorie.Id;
        }

        await _audit.EnregistrerAsync(
            Domain.Enums.AuditAction.Creation, type.Libelle(), identifiant.ToString(), nom,
            cancellationToken: cancellationToken);

        return (await ListerAsync(type, true, cancellationToken)).First(e => e.Id == identifiant);
    }

    public async Task<ElementReferentielDto> ModifierAsync(
        TypeReferentiel type, int id, ElementReferentielRequete requete,
        CancellationToken cancellationToken = default)
    {
        var nom = requete.Nom.Trim();

        if (string.IsNullOrWhiteSpace(nom))
        {
            throw new RegleMetierException("Le nom de la catégorie est obligatoire.");
        }

        await VerifierNomLibreAsync(type, nom, id, cancellationToken);

        if (type == TypeReferentiel.CategorieProduit)
        {
            var categorie = await _context.ProductCategories
                                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                            ?? throw IntrouvableException.Pour(type.Libelle(), id);

            categorie.Name = nom;
            categorie.Description = Nettoyer(requete.Description);
            categorie.IsActive = requete.Actif;
        }
        else
        {
            var categorie = await _context.ExpenseCategories
                                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                            ?? throw IntrouvableException.Pour(type.Libelle(), id);

            categorie.Name = nom;
            categorie.Description = Nettoyer(requete.Description);
            categorie.IsActive = requete.Actif;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(
            Domain.Enums.AuditAction.Modification, type.Libelle(), id.ToString(), nom,
            cancellationToken: cancellationToken);

        return (await ListerAsync(type, true, cancellationToken)).First(e => e.Id == id);
    }

    public async Task SupprimerAsync(
        TypeReferentiel type, int id, CancellationToken cancellationToken = default)
    {
        var element = (await ListerAsync(type, true, cancellationToken)).FirstOrDefault(e => e.Id == id)
                      ?? throw IntrouvableException.Pour(type.Libelle(), id);

        if (element.Systeme)
        {
            throw new RegleMetierException(
                $"« {element.Nom} » est fournie avec le logiciel et ne peut pas être supprimée. " +
                "Vous pouvez la désactiver.");
        }

        if (element.NombreUtilisations > 0)
        {
            throw new RegleMetierException(
                $"« {element.Nom} » est employée par {element.NombreUtilisations} fiche(s) et ne peut " +
                "pas être supprimée. Désactivez-la : elle disparaîtra des listes de choix sans " +
                "rompre les fiches existantes.");
        }

        if (type == TypeReferentiel.CategorieProduit)
        {
            _context.ProductCategories.Remove(
                await _context.ProductCategories.FirstAsync(c => c.Id == id, cancellationToken));
        }
        else
        {
            _context.ExpenseCategories.Remove(
                await _context.ExpenseCategories.FirstAsync(c => c.Id == id, cancellationToken));
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(
            Domain.Enums.AuditAction.Suppression, type.Libelle(), id.ToString(), element.Nom,
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ModeReglementDto>> ListerModesReglementAsync(
        CancellationToken cancellationToken = default)
        => await _context.PaymentMethods
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new ModeReglementDto(
                m.Id, m.Code, m.Name, m.RequiresReference, m.IsActive))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Deux catégories du même nom rendraient les listes de choix illisibles
    /// et fausseraient les totaux par catégorie dans les rapports.
    /// </summary>
    private async Task VerifierNomLibreAsync(
        TypeReferentiel type, string nom, int? saufId, CancellationToken cancellationToken)
    {
        var existe = (await ListerAsync(type, true, cancellationToken))
            .Any(e => e.Id != saufId && string.Equals(e.Nom, nom, StringComparison.CurrentCultureIgnoreCase));

        if (existe)
        {
            throw new RegleMetierException($"Une catégorie « {nom} » existe déjà.");
        }
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();
}
