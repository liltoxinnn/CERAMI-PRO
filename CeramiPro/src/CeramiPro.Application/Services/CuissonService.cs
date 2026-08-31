using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Firing;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Lots de cuisson : quelles pièces dans quel four, à quelle température,
/// pendant combien de temps et pour quel coût d'électricité.
/// Le coût énergétique est réparti entre les pièces au prorata des quantités,
/// afin d'alimenter le coût de revient réel de chaque production.
/// </summary>
public class CuissonService : ICuissonService
{
    private readonly IApplicationDbContext _context;
    private readonly IReferenceNumberService _numerotation;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IServiceDateHeure _horloge;
    private readonly IAuditService _audit;

    public CuissonService(
        IApplicationDbContext context,
        IReferenceNumberService numerotation,
        IUtilisateurCourant utilisateurCourant,
        IServiceDateHeure horloge,
        IAuditService audit)
    {
        _context = context;
        _numerotation = numerotation;
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
        _audit = audit;
    }

    public async Task<PagedResult<CuissonDto>> ListerAsync(
        FiltreCuissonsRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = ChargerAvecDetails().AsNoTracking();

        if (requete.FourId is not null)
        {
            requeteBase = requeteBase.Where(b => b.KilnId == requete.FourId);
        }

        if (requete.Statut is not null)
        {
            requeteBase = requeteBase.Where(b => b.Status == requete.Statut);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(b =>
                b.BatchNumber.ToLower().Contains(recherche) || b.Kiln.Name.ToLower().Contains(recherche));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var cuissons = await requeteBase
            .OrderByDescending(b => b.StartTime).ThenByDescending(b => b.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        return new PagedResult<CuissonDto>(
            cuissons.Select(Convertir).ToList(), total, requete.Page, requete.TaillePage);
    }

    public async Task<CuissonDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
    {
        var cuisson = await ChargerAvecDetails().AsNoTracking()
                          .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                      ?? throw IntrouvableException.Pour("Cuisson", id);

        return Convertir(cuisson);
    }

    public async Task<CuissonDto> CreerAsync(
        CuissonRequete requete, CancellationToken cancellationToken = default)
    {
        var four = await _context.Kilns.FirstOrDefaultAsync(k => k.Id == requete.FourId, cancellationToken)
                   ?? throw new RegleMetierException("Le four sélectionné n'existe pas.");

        if (!four.IsActive || four.Status == KilnStatus.HorsService)
        {
            throw new RegleMetierException($"Le four « {four.Name} » n'est pas disponible.");
        }

        if (requete.Pieces.Count == 0)
        {
            throw new RegleMetierException("Ajoutez au moins une pièce à enfourner.");
        }

        if (requete.Temperature < four.MinTemperature || requete.Temperature > four.MaxTemperature)
        {
            throw new RegleMetierException(
                $"La température doit être comprise entre {four.MinTemperature:0} °C et {four.MaxTemperature:0} °C " +
                $"pour le four « {four.Name} ».");
        }

        var quantiteTotale = requete.Pieces.Sum(p => p.Quantite);

        if (quantiteTotale <= 0)
        {
            throw new RegleMetierException("Indiquez les quantités à enfourner.");
        }

        if (quantiteTotale > four.Capacity)
        {
            throw new RegleMetierException(
                $"La capacité du four « {four.Name} » est de " +
                $"{Formatage.Quantite(four.Capacity)} pièce(s). " +
                $"Vous tentez d'en enfourner {Formatage.Quantite(quantiteTotale)}.");
        }

        var cuisson = new FiringBatch
        {
            BatchNumber = await _numerotation.GenererAsync(TypeDocument.Cuisson, cancellationToken),
            KilnId = four.Id,
            FiringType = requete.Type,
            Status = FiringBatchStatus.Planifiee,
            Temperature = requete.Temperature,
            StartTime = requete.Debut ?? _horloge.MaintenantUtc,
            EnergyCost = requete.CoutEnergie,
            Observations = Nettoyer(requete.Observations),
            UserId = _utilisateurCourant.UtilisateurId
        };

        foreach (var piece in requete.Pieces.Where(p => p.Quantite > 0))
        {
            if (!await _context.Products.AnyAsync(p => p.Id == piece.ProduitId, cancellationToken))
            {
                throw new RegleMetierException("Un des produits sélectionnés n'existe pas.");
            }

            cuisson.Items.Add(new FiringBatchItem
            {
                ProductionOrderId = piece.ProductionId,
                ProductId = piece.ProduitId,
                Quantity = piece.Quantite,
                Notes = Nettoyer(piece.Notes)
            });
        }

        _context.FiringBatches.Add(cuisson);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(FiringBatch), cuisson.Id.ToString(),
            $"Création de la cuisson {cuisson.BatchNumber} dans le four « {four.Name} ».", null, cancellationToken);

        return await ObtenirAsync(cuisson.Id, cancellationToken);
    }

    public async Task<CuissonDto> DemarrerAsync(int id, CancellationToken cancellationToken = default)
    {
        var cuisson = await ChargerAvecDetails().FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                      ?? throw IntrouvableException.Pour("Cuisson", id);

        if (cuisson.Status != FiringBatchStatus.Planifiee)
        {
            throw new RegleMetierException($"La cuisson {cuisson.BatchNumber} est déjà démarrée.");
        }

        cuisson.Status = FiringBatchStatus.EnCours;
        cuisson.StartTime = _horloge.MaintenantUtc;
        cuisson.Kiln.Status = KilnStatus.EnCuisson;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(FiringBatch), id.ToString(),
            $"Démarrage de la cuisson {cuisson.BatchNumber}.", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<CuissonDto> DefournerAsync(
        int id, DefournementRequete requete, CancellationToken cancellationToken = default)
    {
        var cuisson = await ChargerAvecDetails().FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                      ?? throw IntrouvableException.Pour("Cuisson", id);

        if (cuisson.Status is FiringBatchStatus.Terminee or FiringBatchStatus.Annulee)
        {
            throw new RegleMetierException(
                $"La cuisson {cuisson.BatchNumber} est « {cuisson.Status.Libelle()} ».");
        }

        var fin = requete.Fin ?? _horloge.MaintenantUtc;

        if (fin < cuisson.StartTime)
        {
            throw new RegleMetierException("L'heure de fin doit suivre l'heure de début.");
        }

        if (requete.CoutEnergie > 0)
        {
            cuisson.EnergyCost = requete.CoutEnergie;
        }

        foreach (var resultat in requete.Pieces)
        {
            var piece = cuisson.Items.FirstOrDefault(i => i.Id == resultat.PieceId)
                        ?? throw new RegleMetierException("Une ligne de défournement ne correspond à aucune pièce.");

            if (resultat.QuantiteAcceptee + resultat.QuantiteEndommagee > piece.Quantity)
            {
                throw new RegleMetierException(
                    $"Pour « {piece.Product.Name} », le total accepté et endommagé dépasse la quantité enfournée " +
                    $"({Formatage.Quantite(piece.Quantity)}).");
            }

            piece.AcceptedQuantity = resultat.QuantiteAcceptee;
            piece.DamagedQuantity = resultat.QuantiteEndommagee;
            piece.Notes = Nettoyer(resultat.Notes) ?? piece.Notes;
        }

        // Le coût énergétique se répartit au prorata des pièces enfournées.
        var quantiteTotale = cuisson.Items.Sum(i => i.Quantity);

        foreach (var piece in cuisson.Items)
        {
            piece.AllocatedEnergyCost = quantiteTotale > 0
                ? Math.Round(cuisson.EnergyCost * piece.Quantity / quantiteTotale, 2)
                : 0m;

            // La part de cuisson remonte dans le coût de revient de la production.
            if (piece.ProductionOrderId is not null)
            {
                var ordre = await _context.ProductionOrders
                    .FirstOrDefaultAsync(o => o.Id == piece.ProductionOrderId, cancellationToken);

                if (ordre is not null)
                {
                    ordre.FiringCost += piece.AllocatedEnergyCost;
                    ordre.DamagedQuantity += piece.DamagedQuantity;
                }
            }
        }

        cuisson.EndTime = fin;
        cuisson.DamagedQuantity = cuisson.Items.Sum(i => i.DamagedQuantity);
        cuisson.Status = FiringBatchStatus.Terminee;
        cuisson.Observations = Nettoyer(requete.Observations) ?? cuisson.Observations;
        cuisson.Kiln.Status = KilnStatus.Disponible;

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(FiringBatch), id.ToString(),
            $"Défournement de {cuisson.BatchNumber} : " +
            $"{Formatage.Quantite(cuisson.DamagedQuantity)} pièce(s) endommagée(s), " +
            $"coût énergétique {Formatage.Montant(cuisson.EnergyCost)}.", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<CuissonDto> AnnulerAsync(
        int id, string motif, CancellationToken cancellationToken = default)
    {
        var cuisson = await ChargerAvecDetails().FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                      ?? throw IntrouvableException.Pour("Cuisson", id);

        if (cuisson.Status == FiringBatchStatus.Terminee)
        {
            throw new RegleMetierException(
                $"La cuisson {cuisson.BatchNumber} est terminée : elle ne peut plus être annulée.");
        }

        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new RegleMetierException("Indiquez le motif de l'annulation.");
        }

        cuisson.Status = FiringBatchStatus.Annulee;
        cuisson.Observations = $"Annulée : {motif.Trim()}";
        cuisson.Kiln.Status = KilnStatus.Disponible;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Annulation, nameof(FiringBatch), id.ToString(),
            $"Annulation de la cuisson {cuisson.BatchNumber} : {motif.Trim()}", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    private IQueryable<FiringBatch> ChargerAvecDetails()
        => _context.FiringBatches
            .Include(b => b.Kiln)
            .Include(b => b.User)
            .Include(b => b.Items).ThenInclude(i => i.Product)
            .Include(b => b.Items).ThenInclude(i => i.ProductionOrder);

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static CuissonDto Convertir(FiringBatch b) => new(
        b.Id,
        b.BatchNumber,
        b.KilnId,
        b.Kiln.Name,
        b.FiringType,
        b.FiringType.Libelle(),
        b.Status,
        b.Status.Libelle(),
        b.Temperature,
        b.StartTime,
        b.EndTime,
        b.DurationHours,
        b.EnergyCost,
        b.DamagedQuantity,
        b.Observations,
        b.User?.FullName,
        b.Items.Sum(i => i.Quantity),
        b.Items.Select(i => new PieceCuissonDto(
            i.Id, i.ProductionOrderId, i.ProductionOrder?.ProductionNumber,
            i.ProductId, i.Product.Name, i.Quantity, i.AcceptedQuantity, i.DamagedQuantity,
            i.AllocatedEnergyCost, i.Notes)).ToList());
}
