using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Materials;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Unités de mesure : kilogramme, gramme, litre, pièce, mètre… ainsi que les
/// unités personnalisées créées par l'atelier.
/// </summary>
public class UniteService : IUniteService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;

    public UniteService(IApplicationDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<IReadOnlyList<UniteDto>> ListerAsync(
        bool inclureInactives = true, CancellationToken cancellationToken = default)
    {
        var unites = await _context.Units
            .AsNoTracking()
            .Where(u => inclureInactives || u.IsActive)
            .OrderBy(u => u.Type).ThenBy(u => u.Name)
            .Select(u => new
            {
                u.Id, u.Code, u.Name, u.Type, u.ConversionFactor, u.IsSystem, u.IsActive,
                Utilisations = u.Materials.Count
            })
            .ToListAsync(cancellationToken);

        return unites
            .Select(u => new UniteDto(u.Id, u.Code, u.Name, u.Type, u.Type.Libelle(),
                u.ConversionFactor, u.IsSystem, u.IsActive, u.Utilisations))
            .ToList();
    }

    public async Task<UniteDto> CreerAsync(UniteRequete requete, CancellationToken cancellationToken = default)
    {
        var code = requete.Code.Trim();
        await VerifierCodeLibreAsync(code, null, cancellationToken);
        VerifierFacteur(requete.FacteurConversion);

        var unite = new Unit
        {
            Code = code,
            Name = requete.Nom.Trim(),
            Type = requete.Type,
            ConversionFactor = requete.FacteurConversion,
            IsActive = requete.Actif
        };

        _context.Units.Add(unite);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(Unit), unite.Id.ToString(),
            $"Création de l'unité « {unite.Name} ».", null, cancellationToken);

        return (await ListerAsync(true, cancellationToken)).First(u => u.Id == unite.Id);
    }

    public async Task<UniteDto> ModifierAsync(
        int id, UniteRequete requete, CancellationToken cancellationToken = default)
    {
        var unite = await _context.Units.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Unité de mesure", id);

        var code = requete.Code.Trim();
        await VerifierCodeLibreAsync(code, id, cancellationToken);
        VerifierFacteur(requete.FacteurConversion);

        if (unite.IsSystem && !string.Equals(unite.Code, code, StringComparison.Ordinal))
        {
            throw new RegleMetierException(
                $"Le code de l'unité « {unite.Name} », livrée avec le logiciel, ne peut pas être modifié.");
        }

        unite.Code = code;
        unite.Name = requete.Nom.Trim();
        unite.Type = requete.Type;
        unite.ConversionFactor = requete.FacteurConversion;
        unite.IsActive = requete.Actif;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(Unit), id.ToString(),
            $"Modification de l'unité « {unite.Name} ».", null, cancellationToken);

        return (await ListerAsync(true, cancellationToken)).First(u => u.Id == id);
    }

    public async Task SupprimerAsync(int id, CancellationToken cancellationToken = default)
    {
        var unite = await _context.Units.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Unité de mesure", id);

        if (unite.IsSystem)
        {
            throw new RegleMetierException(
                $"L'unité « {unite.Name} » est livrée avec le logiciel : elle peut être désactivée mais pas supprimée.");
        }

        var utilisee = await _context.Materials.AnyAsync(m => m.UnitId == id, cancellationToken)
                       || await _context.ProductRecipeItems.AnyAsync(i => i.UnitId == id, cancellationToken)
                       || await _context.PurchaseItems.AnyAsync(i => i.UnitId == id, cancellationToken);

        if (utilisee)
        {
            throw new RegleMetierException(
                $"L'unité « {unite.Name} » est utilisée : désactivez-la au lieu de la supprimer.");
        }

        _context.Units.Remove(unite);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Suppression, nameof(Unit), id.ToString(),
            $"Suppression de l'unité « {unite.Name} ».", null, cancellationToken);
    }

    private static void VerifierFacteur(decimal facteur)
    {
        if (facteur <= 0)
        {
            throw new RegleMetierException("Le facteur de conversion doit être supérieur à zéro.");
        }
    }

    private async Task VerifierCodeLibreAsync(string code, int? idExclu, CancellationToken cancellationToken)
    {
        var existe = await _context.Units
            .AnyAsync(u => u.Id != idExclu && u.Code.ToLower() == code.ToLower(), cancellationToken);

        if (existe)
        {
            throw new RegleMetierException($"Le code d'unité « {code} » est déjà utilisé.");
        }
    }
}
