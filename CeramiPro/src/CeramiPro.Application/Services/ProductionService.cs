using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Production;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Suit une série de pièces du façonnage au produit fini.
/// Les matières sont vérifiées puis consommées au lancement (règles n°5 et n°7),
/// chaque étape est datée (règle n°8) et le passage à « Terminé » exige un
/// contrôle qualité conforme (règle n°10).
/// </summary>
public class ProductionService : IProductionService
{
    /// <summary>Enchaînement normal des étapes de fabrication.</summary>
    public static readonly IReadOnlyList<ProductionStatus> Etapes = new[]
    {
        ProductionStatus.Planifie,
        ProductionStatus.Preparation,
        ProductionStatus.Faconnage,
        ProductionStatus.Sechage,
        ProductionStatus.PremiereCuisson,
        ProductionStatus.Decoration,
        ProductionStatus.CuissonFinale,
        ProductionStatus.ControleQualite,
        ProductionStatus.Termine
    };

    private readonly IApplicationDbContext _context;
    private readonly IInventaireService _inventaire;
    private readonly IRecetteService _recettes;
    private readonly IReferenceNumberService _numerotation;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IServiceDateHeure _horloge;
    private readonly IAuditService _audit;

    public ProductionService(
        IApplicationDbContext context,
        IInventaireService inventaire,
        IRecetteService recettes,
        IReferenceNumberService numerotation,
        IUtilisateurCourant utilisateurCourant,
        IServiceDateHeure horloge,
        IAuditService audit)
    {
        _context = context;
        _inventaire = inventaire;
        _recettes = recettes;
        _numerotation = numerotation;
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
        _audit = audit;
    }

    public async Task<PagedResult<OrdreProductionDto>> ListerAsync(
        FiltreProductionsRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = ChargerAvecDetails().AsNoTracking();

        if (requete.Statut is not null)
        {
            requeteBase = requeteBase.Where(o => o.Status == requete.Statut);
        }

        if (requete.ProduitId is not null)
        {
            requeteBase = requeteBase.Where(o => o.ProductId == requete.ProduitId);
        }

        if (requete.EmployeId is not null)
        {
            requeteBase = requeteBase.Where(o => o.AssignedUserId == requete.EmployeId);
        }

        if (requete.SeulementEnCours)
        {
            requeteBase = requeteBase.Where(o =>
                o.Status != ProductionStatus.Termine && o.Status != ProductionStatus.Annule);
        }

        if (requete.SeulementEnRetard)
        {
            var maintenant = _horloge.MaintenantUtc;
            requeteBase = requeteBase.Where(o =>
                o.PlannedEndDate != null && o.PlannedEndDate < maintenant
                && o.Status != ProductionStatus.Termine && o.Status != ProductionStatus.Annule);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(o =>
                o.ProductionNumber.ToLower().Contains(recherche) ||
                o.Product.Name.ToLower().Contains(recherche) ||
                o.Product.Reference.ToLower().Contains(recherche));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var ordres = await requeteBase
            .OrderByDescending(o => o.Priority).ThenBy(o => o.PlannedEndDate).ThenByDescending(o => o.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        return new PagedResult<OrdreProductionDto>(
            ordres.Select(Convertir).ToList(), total, requete.Page, requete.TaillePage);
    }

    public async Task<OrdreProductionDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
    {
        var ordre = await ChargerAvecDetails().AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Ordre de production", id);

        return Convertir(ordre);
    }

    public async Task<IReadOnlyList<ColonneProductionDto>> TableauAsync(
        CancellationToken cancellationToken = default)
    {
        var ordres = await ChargerAvecDetails().AsNoTracking()
            .Where(o => o.Status != ProductionStatus.Annule && o.Status != ProductionStatus.Termine)
            .OrderByDescending(o => o.Priority).ThenBy(o => o.PlannedEndDate)
            .ToListAsync(cancellationToken);

        var convertis = ordres.Select(Convertir).ToList();

        return Etapes
            .Where(e => e != ProductionStatus.Termine)
            .Select(etape =>
            {
                var colonne = convertis.Where(o => o.Statut == etape).ToList();
                return new ColonneProductionDto(
                    etape,
                    etape.Libelle(),
                    colonne.Sum(o => o.QuantitePrevue - o.QuantiteTerminee - o.QuantiteEndommagee),
                    colonne);
            })
            .ToList();
    }

    public async Task<SyntheseProductionDto> SyntheseAsync(CancellationToken cancellationToken = default)
    {
        var maintenant = _horloge.MaintenantUtc;

        var ordres = await _context.ProductionOrders
            .AsNoTracking()
            .Select(o => new
            {
                o.Status, o.PlannedEndDate, o.PlannedQuantity, o.CompletedQuantity, o.DamagedQuantity
            })
            .ToListAsync(cancellationToken);

        var actifs = ordres
            .Where(o => o.Status != ProductionStatus.Termine && o.Status != ProductionStatus.Annule)
            .ToList();

        return new SyntheseProductionDto(
            actifs.Count,
            actifs.Count(o => o.Status == ProductionStatus.Sechage),
            actifs.Count(o => o.Status is ProductionStatus.PremiereCuisson or ProductionStatus.CuissonFinale),
            actifs.Count(o => o.Status == ProductionStatus.Decoration),
            actifs.Count(o => o.Status == ProductionStatus.ControleQualite),
            ordres.Count(o => o.Status == ProductionStatus.Termine),
            actifs.Count(o => o.PlannedEndDate is not null && o.PlannedEndDate < maintenant),
            actifs.Sum(o => o.PlannedQuantity - o.CompletedQuantity - o.DamagedQuantity));
    }

    public async Task<OrdreProductionDto> CreerAsync(
        OrdreProductionRequete requete, CancellationToken cancellationToken = default)
    {
        await VerifierAsync(requete, cancellationToken);

        var recetteId = requete.RecetteId ?? await _context.ProductRecipes
            .Where(r => r.ProductId == requete.ProduitId && r.IsDefault && r.IsActive)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var ordre = new ProductionOrder
        {
            ProductionNumber = await _numerotation.GenererAsync(TypeDocument.Production, cancellationToken),
            ProductId = requete.ProduitId,
            ProductRecipeId = recetteId,
            CustomOrderId = requete.CommandeId,
            PlannedQuantity = requete.QuantitePrevue,
            Priority = requete.Priorite,
            Status = ProductionStatus.Planifie,
            PlannedStartDate = requete.DateDebutPrevue ?? _horloge.MaintenantUtc,
            PlannedEndDate = requete.DateFinPrevue,
            AssignedUserId = requete.EmployeId,
            Notes = Nettoyer(requete.Notes),
            LaborCost = requete.CoutMainOeuvre,
            PackagingCost = requete.CoutEmballage,
            OtherCost = requete.AutresCouts
        };

        // Les matières prévues proviennent de la recette, au prorata de la quantité demandée.
        if (recetteId is not null)
        {
            var besoins = await _recettes.CalculerBesoinsAsync(recetteId.Value, requete.QuantitePrevue, cancellationToken);
            var unites = await _context.ProductRecipeItems
                .Where(i => i.ProductRecipeId == recetteId)
                .ToDictionaryAsync(i => i.MaterialId, i => i.UnitId, cancellationToken);

            foreach (var besoin in besoins.Besoins)
            {
                ordre.Materials.Add(new ProductionMaterial
                {
                    MaterialId = besoin.MatiereId,
                    UnitId = unites.TryGetValue(besoin.MatiereId, out var uniteId) ? uniteId : 0,
                    PlannedQuantity = besoin.QuantiteNecessaire,
                    UnitCost = besoin.CoutUnitaire,
                    TotalCost = besoin.Cout
                });
            }

            ordre.EstimatedMaterialCost = besoins.CoutMatieres;
        }

        _context.ProductionOrders.Add(ordre);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(ProductionOrder), ordre.Id.ToString(),
            $"Création de la production {ordre.ProductionNumber} " +
            $"({Formatage.Quantite(ordre.PlannedQuantity)} pièce(s)).", null, cancellationToken);

        return await ObtenirAsync(ordre.Id, cancellationToken);
    }

    public async Task<OrdreProductionDto> ModifierAsync(
        int id, OrdreProductionRequete requete, CancellationToken cancellationToken = default)
    {
        var ordre = await ChargerAvecDetails().FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Ordre de production", id);

        if (ordre.Status != ProductionStatus.Planifie)
        {
            throw new RegleMetierException(
                $"La production {ordre.ProductionNumber} est déjà lancée : " +
                "seule une production planifiée peut être modifiée.");
        }

        await VerifierAsync(requete, cancellationToken);

        ordre.PlannedQuantity = requete.QuantitePrevue;
        ordre.Priority = requete.Priorite;
        ordre.PlannedStartDate = requete.DateDebutPrevue ?? ordre.PlannedStartDate;
        ordre.PlannedEndDate = requete.DateFinPrevue;
        ordre.AssignedUserId = requete.EmployeId;
        ordre.Notes = Nettoyer(requete.Notes);
        ordre.LaborCost = requete.CoutMainOeuvre;
        ordre.PackagingCost = requete.CoutEmballage;
        ordre.OtherCost = requete.AutresCouts;

        // Les besoins en matières sont recalculés pour la nouvelle quantité.
        if (ordre.ProductRecipeId is not null)
        {
            _context.ProductionMaterials.RemoveRange(ordre.Materials);
            ordre.Materials.Clear();

            var besoins = await _recettes.CalculerBesoinsAsync(
                ordre.ProductRecipeId.Value, requete.QuantitePrevue, cancellationToken);
            var unites = await _context.ProductRecipeItems
                .Where(i => i.ProductRecipeId == ordre.ProductRecipeId)
                .ToDictionaryAsync(i => i.MaterialId, i => i.UnitId, cancellationToken);

            foreach (var besoin in besoins.Besoins)
            {
                ordre.Materials.Add(new ProductionMaterial
                {
                    MaterialId = besoin.MatiereId,
                    UnitId = unites.TryGetValue(besoin.MatiereId, out var uniteId) ? uniteId : 0,
                    PlannedQuantity = besoin.QuantiteNecessaire,
                    UnitCost = besoin.CoutUnitaire,
                    TotalCost = besoin.Cout
                });
            }

            ordre.EstimatedMaterialCost = besoins.CoutMatieres;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(ProductionOrder), id.ToString(),
            $"Modification de la production {ordre.ProductionNumber}.", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<OrdreProductionDto> LancerAsync(
        int id, LancementProductionRequete requete, CancellationToken cancellationToken = default)
    {
        var ordre = await ChargerAvecDetails().FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Ordre de production", id);

        if (ordre.Status != ProductionStatus.Planifie)
        {
            throw new RegleMetierException($"La production {ordre.ProductionNumber} est déjà lancée.");
        }

        if (ordre.Materials.Count == 0)
        {
            throw new RegleMetierException(
                "Aucune matière n'est associée à cette production. " +
                "Créez une recette pour ce produit avant de lancer la fabrication.");
        }

        // Règle métier n°7 : le contrôle des matières précède toute consommation.
        var manquants = ordre.Materials
            .Where(m => m.Material.CurrentQuantity < m.PlannedQuantity)
            .Select(m =>
                $"{m.Material.Name} — nécessaire : " +
                $"{Formatage.Quantite(m.PlannedQuantity, m.Material.Unit.Code)}, " +
                $"disponible : {Formatage.Quantite(m.Material.CurrentQuantity, m.Material.Unit.Code)}")
            .ToList();

        if (manquants.Count > 0)
        {
            var derogation = requete.ForcerMalgreStockInsuffisant
                             && _utilisateurCourant.PossedeDroit(PermissionCodes.ProductionDeroger);

            if (!derogation)
            {
                throw new RegleMetierException("Matières insuffisantes pour lancer cette production.", manquants);
            }

            if (string.IsNullOrWhiteSpace(requete.MotifDerogation))
            {
                throw new RegleMetierException("Indiquez le motif de la dérogation.");
            }

            ordre.StockCheckOverridden = true;
            ordre.OverriddenByUserId = _utilisateurCourant.UtilisateurId;
            ordre.OverrideReason = requete.MotifDerogation.Trim();
        }

        foreach (var matiere in ordre.Materials)
        {
            await _inventaire.EnregistrerAsync(new MouvementStockRequete
            {
                TypeArticle = InventoryItemType.MatierePremiere,
                TypeMouvement = InventoryTransactionType.ConsommationProduction,
                MatiereId = matiere.MaterialId,
                Quantite = -matiere.PlannedQuantity,
                CoutUnitaire = matiere.Material.AverageCost,
                ProductionId = ordre.Id,
                Reference = ordre.ProductionNumber,
                AutoriserStockNegatif = ordre.StockCheckOverridden
            }, cancellationToken);

            matiere.ConsumedQuantity = matiere.PlannedQuantity;
            matiere.UnitCost = matiere.Material.AverageCost;
            matiere.TotalCost = Math.Round(matiere.PlannedQuantity * matiere.Material.AverageCost, 2);
        }

        ordre.MaterialsConsumed = true;
        ordre.ActualMaterialCost = Math.Round(ordre.Materials.Sum(m => m.TotalCost), 2);
        ordre.ActualStartDate = _horloge.MaintenantUtc;
        ordre.Status = ProductionStatus.Preparation;

        AjouterEtape(ordre, ProductionStatus.Preparation, ordre.PlannedQuantity, 0m,
            _utilisateurCourant.UtilisateurId, "Lancement de la production et consommation des matières.");

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(ProductionOrder), id.ToString(),
            $"Lancement de la production {ordre.ProductionNumber} : " +
            $"{Formatage.Montant(ordre.ActualMaterialCost)} de matières consommées." +
            (ordre.StockCheckOverridden ? $" Dérogation : {ordre.OverrideReason}" : string.Empty),
            null, cancellationToken);

        if (ordre.StockCheckOverridden)
        {
            await _audit.EnregistrerAsync(AuditAction.Derogation, nameof(ProductionOrder), id.ToString(),
                $"Dérogation de stock sur {ordre.ProductionNumber} : {ordre.OverrideReason}",
                null, cancellationToken);
        }

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<OrdreProductionDto> ChangerEtapeAsync(
        int id, ChangementEtapeRequete requete, CancellationToken cancellationToken = default)
    {
        var ordre = await ChargerAvecDetails().FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Ordre de production", id);

        if (ordre.Status is ProductionStatus.Termine or ProductionStatus.Annule)
        {
            throw new RegleMetierException(
                $"La production {ordre.ProductionNumber} est « {ordre.Status.Libelle()} » : elle ne peut plus avancer.");
        }

        if (ordre.Status == ProductionStatus.Planifie)
        {
            throw new RegleMetierException(
                "Lancez d'abord la production : les matières doivent être consommées avant la fabrication.");
        }

        var positionActuelle = Etapes.ToList().IndexOf(ordre.Status);
        var positionCible = Etapes.ToList().IndexOf(requete.NouvelleEtape);

        if (positionCible <= positionActuelle)
        {
            throw new RegleMetierException(
                $"La production est déjà à l'étape « {ordre.Status.Libelle()} » : " +
                "elle ne peut avancer que vers une étape suivante.");
        }

        if (requete.QuantiteEndommagee < 0 || requete.QuantiteAcceptee < 0)
        {
            throw new RegleMetierException("Les quantités ne peuvent pas être négatives.");
        }

        var restantes = ordre.PlannedQuantity - ordre.DamagedQuantity;

        if (requete.QuantiteEndommagee > restantes)
        {
            throw new RegleMetierException(
                $"La quantité endommagée dépasse les pièces encore en fabrication " +
                $"({Formatage.Quantite(restantes)}).");
        }

        // Règle métier n°10 : « Terminé » n'est accessible qu'après un contrôle qualité conforme.
        if (requete.NouvelleEtape == ProductionStatus.Termine)
        {
            var controle = await _context.QualityChecks
                .Where(q => q.ProductionOrderId == id)
                .OrderByDescending(q => q.CheckedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (controle is null)
            {
                throw new RegleMetierException(
                    "Un contrôle qualité est obligatoire avant de terminer la production.");
            }

            if (controle.Result == QualityResult.NonConforme)
            {
                throw new RegleMetierException(
                    "Le dernier contrôle qualité est non conforme : la production ne peut pas être terminée.");
            }
        }

        // Fermeture de l'étape précédente encore ouverte.
        var etapeOuverte = ordre.StageHistory
            .Where(h => h.EndedAt is null)
            .OrderByDescending(h => h.StartedAt)
            .FirstOrDefault();

        if (etapeOuverte is not null)
        {
            etapeOuverte.EndedAt = _horloge.MaintenantUtc;
            etapeOuverte.AcceptedQuantity = requete.QuantiteAcceptee;
            etapeOuverte.DamagedQuantity = requete.QuantiteEndommagee;
            etapeOuverte.Notes = Nettoyer(requete.Notes) ?? etapeOuverte.Notes;
        }

        ordre.DamagedQuantity += requete.QuantiteEndommagee;
        ordre.Status = requete.NouvelleEtape;

        if (requete.EmployeId is not null)
        {
            ordre.AssignedUserId = requete.EmployeId;
        }

        if (requete.NouvelleEtape == ProductionStatus.Termine)
        {
            await TerminerAsync(ordre, requete, cancellationToken);
        }
        else
        {
            AjouterEtape(ordre, requete.NouvelleEtape, 0m, 0m,
                requete.EmployeId ?? _utilisateurCourant.UtilisateurId, Nettoyer(requete.Notes));
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(ProductionOrder), id.ToString(),
            $"Production {ordre.ProductionNumber} : passage à l'étape « {requete.NouvelleEtape.Libelle()} ».",
            null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    /// <summary>Clôture la production et fait entrer les pièces acceptées en stock (règle n°4).</summary>
    private async Task TerminerAsync(
        ProductionOrder ordre, ChangementEtapeRequete requete, CancellationToken cancellationToken)
    {
        var acceptees = requete.QuantiteAcceptee > 0
            ? requete.QuantiteAcceptee
            : ordre.PlannedQuantity - ordre.DamagedQuantity;

        if (acceptees < 0)
        {
            acceptees = 0;
        }

        ordre.CompletedQuantity = acceptees;
        ordre.ActualEndDate = _horloge.MaintenantUtc;

        AjouterEtape(ordre, ProductionStatus.Termine, acceptees, requete.QuantiteEndommagee,
            requete.EmployeId ?? _utilisateurCourant.UtilisateurId, Nettoyer(requete.Notes));

        if (acceptees > 0)
        {
            var coutUnitaire = ordre.TotalCost > 0 ? Math.Round(ordre.TotalCost / acceptees, 4) : 0m;

            await _inventaire.EnregistrerAsync(new MouvementStockRequete
            {
                TypeArticle = InventoryItemType.ProduitFini,
                TypeMouvement = InventoryTransactionType.EntreeProduction,
                ProduitId = ordre.ProductId,
                Quantite = acceptees,
                CoutUnitaire = coutUnitaire,
                ProductionId = ordre.Id,
                Reference = ordre.ProductionNumber,
                Notes = $"Production terminée : {Formatage.Quantite(acceptees)} pièce(s)."
            }, cancellationToken);

            // Le coût de revient réel remplace l'estimation portée par la fiche produit.
            var produit = await _context.Products.FirstAsync(p => p.Id == ordre.ProductId, cancellationToken);
            produit.ProductionCost = Math.Round(coutUnitaire, 2);
        }
    }

    public async Task<OrdreProductionDto> AnnulerAsync(
        int id, string motif, CancellationToken cancellationToken = default)
    {
        var ordre = await ChargerAvecDetails().FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Ordre de production", id);

        if (ordre.Status == ProductionStatus.Annule)
        {
            throw new RegleMetierException($"La production {ordre.ProductionNumber} est déjà annulée.");
        }

        if (ordre.Status == ProductionStatus.Termine)
        {
            throw new RegleMetierException(
                $"La production {ordre.ProductionNumber} est terminée : les pièces sont déjà en stock.");
        }

        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new RegleMetierException("Indiquez le motif de l'annulation.");
        }

        // Les matières consommées retournent en stock (règle métier n°6).
        if (ordre.MaterialsConsumed)
        {
            await _inventaire.AnnulerDocumentAsync(null, null, ordre.Id,
                $"Annulation de la production {ordre.ProductionNumber} : {motif.Trim()}", cancellationToken);

            foreach (var matiere in ordre.Materials)
            {
                matiere.ConsumedQuantity = 0;
            }

            ordre.MaterialsConsumed = false;
            ordre.ActualMaterialCost = 0m;
        }

        ordre.Status = ProductionStatus.Annule;
        ordre.Notes = string.IsNullOrWhiteSpace(ordre.Notes)
            ? $"Annulée : {motif.Trim()}"
            : $"{ordre.Notes}\nAnnulée : {motif.Trim()}";

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Annulation, nameof(ProductionOrder), id.ToString(),
            $"Annulation de la production {ordre.ProductionNumber} : {motif.Trim()}", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    private void AjouterEtape(
        ProductionOrder ordre, ProductionStatus etape, decimal acceptees, decimal endommagees,
        int? employeId, string? notes)
        => ordre.StageHistory.Add(new ProductionStageHistory
        {
            Stage = etape,
            StartedAt = _horloge.MaintenantUtc,
            EndedAt = etape == ProductionStatus.Termine ? _horloge.MaintenantUtc : null,
            UserId = employeId,
            AcceptedQuantity = acceptees,
            DamagedQuantity = endommagees,
            Notes = notes
        });

    private IQueryable<ProductionOrder> ChargerAvecDetails()
        => _context.ProductionOrders
            .Include(o => o.Product)
            .Include(o => o.ProductRecipe)
            .Include(o => o.CustomOrder)
            .Include(o => o.AssignedUser)
            .Include(o => o.Materials).ThenInclude(m => m.Material).ThenInclude(m => m.Unit)
            .Include(o => o.StageHistory).ThenInclude(h => h.User);

    private async Task VerifierAsync(OrdreProductionRequete requete, CancellationToken cancellationToken)
    {
        if (requete.QuantitePrevue <= 0)
        {
            throw new RegleMetierException("La quantité à produire doit être supérieure à zéro.");
        }

        if (!await _context.Products.AnyAsync(p => p.Id == requete.ProduitId, cancellationToken))
        {
            throw new RegleMetierException("Le produit sélectionné n'existe pas.");
        }

        if (requete.RecetteId is not null && !await _context.ProductRecipes
                .AnyAsync(r => r.Id == requete.RecetteId && r.ProductId == requete.ProduitId, cancellationToken))
        {
            throw new RegleMetierException("La recette sélectionnée ne correspond pas à ce produit.");
        }

        if (requete.DateFinPrevue is not null && requete.DateDebutPrevue is not null
            && requete.DateFinPrevue < requete.DateDebutPrevue)
        {
            throw new RegleMetierException("La date de fin prévue doit suivre la date de début.");
        }
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private OrdreProductionDto Convertir(ProductionOrder o) => new(
        o.Id,
        o.ProductionNumber,
        o.ProductId,
        o.Product.Name,
        o.Product.Reference,
        o.ProductRecipeId,
        o.ProductRecipe?.Name,
        o.CustomOrderId,
        o.CustomOrder?.OrderNumber,
        o.PlannedQuantity,
        o.CompletedQuantity,
        o.DamagedQuantity,
        o.Priority,
        o.Priority.Libelle(),
        o.Status,
        o.Status.Libelle(),
        o.PlannedStartDate,
        o.PlannedEndDate,
        o.ActualStartDate,
        o.ActualEndDate,
        o.AssignedUserId,
        o.AssignedUser?.FullName,
        o.Notes,
        o.EstimatedMaterialCost,
        o.ActualMaterialCost,
        o.LaborCost,
        o.FiringCost,
        o.DecorationCost,
        o.PackagingCost,
        o.OtherCost,
        o.TotalCost,
        o.UnitCost,
        o.MaterialsConsumed,
        o.StockCheckOverridden,
        o.OverrideReason,
        o.PlannedEndDate is not null && o.PlannedEndDate < _horloge.MaintenantUtc
        && o.Status != ProductionStatus.Termine && o.Status != ProductionStatus.Annule,
        o.Materials.Select(m => new MatiereProductionDto(
            m.Id, m.MaterialId, m.Material.Name, m.Material.Unit.Code,
            m.PlannedQuantity, m.ConsumedQuantity, m.UnitCost, m.TotalCost)).ToList(),
        o.StageHistory.OrderBy(h => h.StartedAt).Select(h => new EtapeProductionDto(
            h.Id, h.Stage, h.Stage.Libelle(), h.StartedAt, h.EndedAt,
            h.User?.FullName, h.AcceptedQuantity, h.DamagedQuantity, h.Notes)).ToList());
}
