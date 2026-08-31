using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Decoration;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Travaux de décoration : émaillage, peinture, dorure, argenture.
/// Le coût de la décoration remonte dans le coût de revient de la production.
/// </summary>
public class DecorationService : IDecorationService
{
    private readonly IApplicationDbContext _context;
    private readonly IReferenceNumberService _numerotation;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IServiceDateHeure _horloge;
    private readonly IAuditService _audit;

    public DecorationService(
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

    public async Task<PagedResult<DecorationDto>> ListerAsync(
        FiltreDecorationsRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = ChargerAvecDetails().AsNoTracking();

        if (requete.Statut is not null)
        {
            requeteBase = requeteBase.Where(d => d.Status == requete.Statut);
        }

        if (requete.ProductionId is not null)
        {
            requeteBase = requeteBase.Where(d => d.ProductionOrderId == requete.ProductionId);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(d =>
                d.Reference.ToLower().Contains(recherche) ||
                d.DecorationType.Name.ToLower().Contains(recherche) ||
                (d.Colors != null && d.Colors.ToLower().Contains(recherche)));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var decorations = await requeteBase
            .OrderByDescending(d => d.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        return new PagedResult<DecorationDto>(
            decorations.Select(Convertir).ToList(), total, requete.Page, requete.TaillePage);
    }

    public async Task<DecorationDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
    {
        var decoration = await ChargerAvecDetails().AsNoTracking()
                             .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                         ?? throw IntrouvableException.Pour("Décoration", id);

        return Convertir(decoration);
    }

    public async Task<DecorationDto> CreerAsync(
        DecorationRequete requete, CancellationToken cancellationToken = default)
    {
        await VerifierAsync(requete, cancellationToken);

        var decoration = new DecorationOrder
        {
            Reference = await _numerotation.GenererAsync(TypeDocument.Decoration, cancellationToken),
            DecorationTypeId = requete.TypeDecorationId,
            ProductionOrderId = requete.ProductionId,
            CustomOrderId = requete.CommandeId,
            Quantity = requete.Quantite,
            Status = DecorationStatus.Planifiee,
            Colors = Nettoyer(requete.Couleurs),
            Glaze = Nettoyer(requete.Email),
            Paint = Nettoyer(requete.Peinture),
            GoldQuantity = requete.QuantiteOr,
            SilverQuantity = requete.QuantiteArgent,
            MaterialsUsed = Nettoyer(requete.MateriauxUtilises),
            Cost = requete.Cout,
            AssignedUserId = requete.EmployeId ?? _utilisateurCourant.UtilisateurId,
            StartDate = requete.DateDebut,
            EndDate = requete.DateFin,
            Notes = Nettoyer(requete.Notes)
        };

        _context.DecorationOrders.Add(decoration);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(DecorationOrder), decoration.Id.ToString(),
            $"Création du travail de décoration {decoration.Reference}.", null, cancellationToken);

        return await ObtenirAsync(decoration.Id, cancellationToken);
    }

    public async Task<DecorationDto> ModifierAsync(
        int id, DecorationRequete requete, CancellationToken cancellationToken = default)
    {
        var decoration = await ChargerAvecDetails().FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                         ?? throw IntrouvableException.Pour("Décoration", id);

        if (decoration.Status == DecorationStatus.Terminee)
        {
            throw new RegleMetierException(
                $"Le travail {decoration.Reference} est terminé : il ne peut plus être modifié.");
        }

        await VerifierAsync(requete, cancellationToken);

        decoration.DecorationTypeId = requete.TypeDecorationId;
        decoration.Quantity = requete.Quantite;
        decoration.Colors = Nettoyer(requete.Couleurs);
        decoration.Glaze = Nettoyer(requete.Email);
        decoration.Paint = Nettoyer(requete.Peinture);
        decoration.GoldQuantity = requete.QuantiteOr;
        decoration.SilverQuantity = requete.QuantiteArgent;
        decoration.MaterialsUsed = Nettoyer(requete.MateriauxUtilises);
        decoration.Cost = requete.Cout;
        decoration.AssignedUserId = requete.EmployeId ?? decoration.AssignedUserId;
        decoration.StartDate = requete.DateDebut;
        decoration.EndDate = requete.DateFin;
        decoration.Notes = Nettoyer(requete.Notes);

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(DecorationOrder), id.ToString(),
            $"Modification du travail de décoration {decoration.Reference}.", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<DecorationDto> ChangerStatutAsync(
        int id, DecorationStatus statut, CancellationToken cancellationToken = default)
    {
        var decoration = await ChargerAvecDetails().FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                         ?? throw IntrouvableException.Pour("Décoration", id);

        if (decoration.Status == DecorationStatus.Terminee && statut != DecorationStatus.Terminee)
        {
            throw new RegleMetierException(
                $"Le travail {decoration.Reference} est déjà terminé : son état ne peut plus revenir en arrière.");
        }

        decoration.Status = statut;

        if (statut == DecorationStatus.EnCours && decoration.StartDate is null)
        {
            decoration.StartDate = _horloge.MaintenantUtc;
        }

        if (statut == DecorationStatus.Terminee)
        {
            decoration.EndDate ??= _horloge.MaintenantUtc;

            // Le coût de décoration s'ajoute au coût de revient de la production.
            if (decoration.ProductionOrderId is not null)
            {
                var ordre = await _context.ProductionOrders
                    .FirstOrDefaultAsync(o => o.Id == decoration.ProductionOrderId, cancellationToken);

                if (ordre is not null)
                {
                    ordre.DecorationCost += decoration.Cost;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(DecorationOrder), id.ToString(),
            $"Travail de décoration {decoration.Reference} : {statut.Libelle()}.", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<DecorationDto> AjouterPhotoAsync(
        int id, string chemin, string? legende, CancellationToken cancellationToken = default)
    {
        if (!await _context.DecorationOrders.AnyAsync(d => d.Id == id, cancellationToken))
        {
            throw IntrouvableException.Pour("Décoration", id);
        }

        if (string.IsNullOrWhiteSpace(chemin))
        {
            throw new RegleMetierException("Sélectionnez une photo à ajouter.");
        }

        _context.DecorationImages.Add(new DecorationImage
        {
            DecorationOrderId = id,
            FilePath = chemin.Trim(),
            Caption = Nettoyer(legende)
        });

        await _context.SaveChangesAsync(cancellationToken);
        return await ObtenirAsync(id, cancellationToken);
    }

    private IQueryable<DecorationOrder> ChargerAvecDetails()
        => _context.DecorationOrders
            .Include(d => d.DecorationType)
            .Include(d => d.ProductionOrder)
            .Include(d => d.CustomOrder)
            .Include(d => d.AssignedUser)
            .Include(d => d.Images);

    private async Task VerifierAsync(DecorationRequete requete, CancellationToken cancellationToken)
    {
        if (requete.Quantite <= 0)
        {
            throw new RegleMetierException("La quantité à décorer doit être supérieure à zéro.");
        }

        if (!await _context.DecorationTypes.AnyAsync(t => t.Id == requete.TypeDecorationId, cancellationToken))
        {
            throw new RegleMetierException("Le type de décoration sélectionné n'existe pas.");
        }

        if (requete.ProductionId is not null && !await _context.ProductionOrders
                .AnyAsync(o => o.Id == requete.ProductionId, cancellationToken))
        {
            throw new RegleMetierException("L'ordre de production sélectionné n'existe pas.");
        }

        if (requete.Cout < 0)
        {
            throw new RegleMetierException("Le coût de la décoration ne peut pas être négatif.");
        }
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static DecorationDto Convertir(DecorationOrder d) => new(
        d.Id,
        d.Reference,
        d.DecorationTypeId,
        d.DecorationType.Name,
        d.ProductionOrderId,
        d.ProductionOrder?.ProductionNumber,
        d.CustomOrderId,
        d.CustomOrder?.OrderNumber,
        d.Quantity,
        d.Status,
        d.Status.Libelle(),
        d.Colors,
        d.Glaze,
        d.Paint,
        d.GoldQuantity,
        d.SilverQuantity,
        d.MaterialsUsed,
        d.Cost,
        d.AssignedUserId,
        d.AssignedUser?.FullName,
        d.StartDate,
        d.EndDate,
        d.Notes,
        d.Images.OrderBy(i => i.SortOrder).Select(i => i.FilePath).ToList());
}
