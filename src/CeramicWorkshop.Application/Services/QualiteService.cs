using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Production;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Quality;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Contrôle qualité obligatoire avant l'entrée en stock des produits finis
/// (règle métier n°10). Huit points sont vérifiés et chaque défaut relevé est
/// enregistré avec sa gravité et la solution retenue.
/// </summary>
public class QualiteService : IQualiteService
{
    private readonly IApplicationDbContext _context;
    private readonly IReferenceNumberService _numerotation;
    private readonly ICurrentUserService _utilisateurCourant;
    private readonly IDateTimeService _horloge;
    private readonly IAuditService _audit;

    public QualiteService(
        IApplicationDbContext context,
        IReferenceNumberService numerotation,
        ICurrentUserService utilisateurCourant,
        IDateTimeService horloge,
        IAuditService audit)
    {
        _context = context;
        _numerotation = numerotation;
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
        _audit = audit;
    }

    public async Task<PagedResult<ControleQualiteDto>> ListerAsync(
        FiltreControlesRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = ChargerAvecDetails().AsNoTracking();

        if (requete.Resultat is not null)
        {
            requeteBase = requeteBase.Where(q => q.Result == requete.Resultat);
        }

        if (requete.ProductionId is not null)
        {
            requeteBase = requeteBase.Where(q => q.ProductionOrderId == requete.ProductionId);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(q =>
                q.Reference.ToLower().Contains(recherche) ||
                (q.ProductionOrder != null && q.ProductionOrder.ProductionNumber.ToLower().Contains(recherche)));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var controles = await requeteBase
            .OrderByDescending(q => q.CheckedAt).ThenByDescending(q => q.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        return new PagedResult<ControleQualiteDto>(
            controles.Select(Convertir).ToList(), total, requete.Page, requete.TaillePage);
    }

    public async Task<ControleQualiteDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
    {
        var controle = await ChargerAvecDetails().AsNoTracking()
                           .FirstOrDefaultAsync(q => q.Id == id, cancellationToken)
                       ?? throw NotFoundException.Pour("Contrôle qualité", id);

        return Convertir(controle);
    }

    public async Task<ControleQualiteDto> EnregistrerAsync(
        ControleQualiteRequete requete, CancellationToken cancellationToken = default)
    {
        if (requete.ProductionId is null && requete.CommandeId is null)
        {
            throw new BusinessRuleException(
                "Indiquez la production ou la commande personnalisée concernée par ce contrôle.");
        }

        if (requete.QuantiteControlee <= 0)
        {
            throw new BusinessRuleException("Indiquez le nombre de pièces contrôlées.");
        }

        var somme = requete.QuantiteAcceptee + requete.QuantiteRefusee + requete.QuantiteARetoucher;

        if (somme > requete.QuantiteControlee)
        {
            throw new BusinessRuleException(
                "Le total des pièces acceptées, refusées et à retoucher dépasse le nombre de pièces contrôlées.");
        }

        if (requete.ProductionId is not null && !await _context.ProductionOrders
                .AnyAsync(o => o.Id == requete.ProductionId, cancellationToken))
        {
            throw new BusinessRuleException("L'ordre de production sélectionné n'existe pas.");
        }

        var pointsConformes = requete.FissuresConformes && requete.FormeConforme && requete.CouleurConforme
                              && requete.EmailConforme && requete.DecorationConforme
                              && requete.DimensionsConformes && requete.SurfaceConforme && requete.CuissonConforme;

        // Le résultat découle des points contrôlés et des quantités constatées.
        var resultat = requete.QuantiteRefusee > 0 || (!pointsConformes && requete.QuantiteARetoucher == 0)
            ? QualityResult.NonConforme
            : requete.QuantiteARetoucher > 0 || !pointsConformes
                ? QualityResult.RetoucheNecessaire
                : QualityResult.Conforme;

        var controle = new QualityCheck
        {
            Reference = await _numerotation.GenererAsync(TypeDocument.Qualite, cancellationToken),
            ProductionOrderId = requete.ProductionId,
            CustomOrderId = requete.CommandeId,
            CheckedAt = _horloge.UtcNow,
            CheckedByUserId = _utilisateurCourant.UserId,
            InspectedQuantity = requete.QuantiteControlee,
            AcceptedQuantity = requete.QuantiteAcceptee,
            RejectedQuantity = requete.QuantiteRefusee,
            ReworkQuantity = requete.QuantiteARetoucher,
            Result = resultat,
            CracksOk = requete.FissuresConformes,
            ShapeOk = requete.FormeConforme,
            ColorOk = requete.CouleurConforme,
            GlazeOk = requete.EmailConforme,
            DecorationOk = requete.DecorationConforme,
            DimensionsOk = requete.DimensionsConformes,
            SurfaceOk = requete.SurfaceConforme,
            FiringOk = requete.CuissonConforme,
            Notes = Nettoyer(requete.Notes)
        };

        foreach (var defaut in requete.Defauts.Where(d => !string.IsNullOrWhiteSpace(d.Description)))
        {
            controle.Issues.Add(new QualityIssue
            {
                CheckPoint = defaut.PointControle,
                Severity = defaut.Gravite,
                Resolution = defaut.Solution,
                Quantity = defaut.Quantite,
                Description = defaut.Description.Trim(),
                Solution = Nettoyer(defaut.Remede)
            });
        }

        _context.QualityChecks.Add(controle);

        // Les pièces refusées comptent comme endommagées sur l'ordre de production.
        if (requete.ProductionId is not null && requete.QuantiteRefusee > 0)
        {
            var ordre = await _context.ProductionOrders
                .FirstAsync(o => o.Id == requete.ProductionId, cancellationToken);
            ordre.DamagedQuantity += requete.QuantiteRefusee;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(QualityCheck), controle.Id.ToString(),
            $"Contrôle qualité {controle.Reference} : {resultat.Libelle()} " +
            $"({MontantFormatter.FormaterQuantite(requete.QuantiteAcceptee)} pièce(s) acceptée(s)).",
            null, cancellationToken);

        return await ObtenirAsync(controle.Id, cancellationToken);
    }

    private IQueryable<QualityCheck> ChargerAvecDetails()
        => _context.QualityChecks
            .Include(q => q.ProductionOrder)
            .Include(q => q.CustomOrder)
            .Include(q => q.CheckedByUser)
            .Include(q => q.Issues);

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static ControleQualiteDto Convertir(QualityCheck q) => new(
        q.Id,
        q.Reference,
        q.ProductionOrderId,
        q.ProductionOrder?.ProductionNumber,
        q.CustomOrderId,
        q.CustomOrder?.OrderNumber,
        q.CheckedAt,
        q.CheckedByUser?.FullName,
        q.InspectedQuantity,
        q.AcceptedQuantity,
        q.RejectedQuantity,
        q.ReworkQuantity,
        q.Result,
        q.Result.Libelle(),
        q.CracksOk, q.ShapeOk, q.ColorOk, q.GlazeOk,
        q.DecorationOk, q.DimensionsOk, q.SurfaceOk, q.FiringOk,
        q.Notes,
        q.Issues.Select(i => new DefautQualiteDto(
            i.Id, i.CheckPoint, i.CheckPoint.Libelle(), i.Severity, i.Severity.Libelle(),
            i.Resolution, i.Resolution.Libelle(), i.Quantity, i.Description, i.Solution)).ToList());
}
