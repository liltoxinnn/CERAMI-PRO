using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Production;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Firing;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>Fours de l'atelier : capacité, plage de températures et disponibilité.</summary>
public class FourService : IFourService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;

    public FourService(IApplicationDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<IReadOnlyList<FourDto>> ListerAsync(CancellationToken cancellationToken = default)
    {
        var fours = await _context.Kilns
            .AsNoTracking()
            .OrderBy(k => k.Name)
            .Select(k => new
            {
                k.Id, k.Reference, k.Name, k.Capacity, k.MinTemperature, k.MaxTemperature,
                k.Location, k.Status, k.Notes, k.IsActive,
                EnCours = k.FiringBatches.Count(b => b.Status == FiringBatchStatus.EnCours)
            })
            .ToListAsync(cancellationToken);

        return fours
            .Select(k => new FourDto(k.Id, k.Reference, k.Name, k.Capacity, k.MinTemperature,
                k.MaxTemperature, k.Location, k.Status, k.Status.Libelle(), k.Notes, k.IsActive, k.EnCours))
            .ToList();
    }

    public async Task<FourDto> CreerAsync(FourRequete requete, CancellationToken cancellationToken = default)
    {
        Verifier(requete);

        var nombre = await _context.Kilns.CountAsync(cancellationToken);

        var four = new Kiln
        {
            Reference = $"FOUR-{nombre + 1:00}",
            Name = requete.Nom.Trim(),
            Capacity = requete.Capacite,
            MinTemperature = requete.TemperatureMin,
            MaxTemperature = requete.TemperatureMax,
            Location = Nettoyer(requete.Emplacement),
            Status = requete.Statut,
            Notes = Nettoyer(requete.Notes),
            IsActive = requete.Actif
        };

        _context.Kilns.Add(four);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(Kiln), four.Id.ToString(),
            $"Création du four « {four.Name} ».", null, cancellationToken);

        return (await ListerAsync(cancellationToken)).First(f => f.Id == four.Id);
    }

    public async Task<FourDto> ModifierAsync(
        int id, FourRequete requete, CancellationToken cancellationToken = default)
    {
        var four = await _context.Kilns.FirstOrDefaultAsync(k => k.Id == id, cancellationToken)
                   ?? throw NotFoundException.Pour("Four", id);

        Verifier(requete);

        four.Name = requete.Nom.Trim();
        four.Capacity = requete.Capacite;
        four.MinTemperature = requete.TemperatureMin;
        four.MaxTemperature = requete.TemperatureMax;
        four.Location = Nettoyer(requete.Emplacement);
        four.Status = requete.Statut;
        four.Notes = Nettoyer(requete.Notes);
        four.IsActive = requete.Actif;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(Kiln), id.ToString(),
            $"Modification du four « {four.Name} ».", null, cancellationToken);

        return (await ListerAsync(cancellationToken)).First(f => f.Id == id);
    }

    public async Task SupprimerAsync(int id, CancellationToken cancellationToken = default)
    {
        var four = await _context.Kilns.FirstOrDefaultAsync(k => k.Id == id, cancellationToken)
                   ?? throw NotFoundException.Pour("Four", id);

        if (await _context.FiringBatches.AnyAsync(b => b.KilnId == id, cancellationToken))
        {
            throw new BusinessRuleException(
                $"Le four « {four.Name} » a déjà servi à des cuissons. Désactivez-le au lieu de le supprimer.");
        }

        _context.Kilns.Remove(four);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Suppression, nameof(Kiln), id.ToString(),
            $"Suppression du four « {four.Name} ».", null, cancellationToken);
    }

    private static void Verifier(FourRequete requete)
    {
        if (requete.Capacite <= 0)
        {
            throw new BusinessRuleException("La capacité du four doit être supérieure à zéro.");
        }

        if (requete.TemperatureMax <= requete.TemperatureMin)
        {
            throw new BusinessRuleException(
                "La température maximale doit être supérieure à la température minimale.");
        }
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();
}
