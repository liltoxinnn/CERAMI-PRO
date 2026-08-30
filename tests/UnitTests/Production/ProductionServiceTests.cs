using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Production;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Enums;
using CeramicWorkshop.UnitTests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.UnitTests.Production;

public class ProductionServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    private async Task<OrdreProductionDto> CreerProductionAsync(int produitId, decimal quantite = 20m)
        => await _atelier.Production.CreerAsync(new OrdreProductionRequete
        {
            ProduitId = produitId,
            QuantitePrevue = quantite,
            CoutMainOeuvre = 600m * quantite,
            CoutEmballage = 50m * quantite
        });

    [Fact]
    public async Task Une_production_reprend_les_matieres_de_la_recette()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();

        var ordre = await CreerProductionAsync(produitId, 20m);

        ordre.Numero.Should().StartWith("PROD-");
        ordre.Statut.Should().Be(ProductionStatus.Planifie);
        ordre.Matieres.Should().HaveCount(2);
        ordre.Matieres.Single(m => m.MatiereNom == "Argile").QuantitePrevue.Should().Be(30m);
        ordre.Matieres.Single(m => m.MatiereNom == "Émail").QuantitePrevue.Should().Be(2m);
    }

    [Fact]
    public async Task La_creation_ne_consomme_pas_encore_les_matieres()
    {
        var (produitId, _, argileId) = await _atelier.PreparerVaseAsync(stockArgile: 100m);

        await CreerProductionAsync(produitId, 20m);

        (await _atelier.StockMatiereAsync(argileId)).Should().Be(100m);
    }

    [Fact]
    public async Task Le_lancement_consomme_les_matieres()
    {
        var (produitId, _, argileId) = await _atelier.PreparerVaseAsync(stockArgile: 100m);
        var ordre = await CreerProductionAsync(produitId, 20m);

        var lance = await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());

        lance.Statut.Should().Be(ProductionStatus.Preparation);
        lance.MatieresConsommees.Should().BeTrue();
        (await _atelier.StockMatiereAsync(argileId)).Should().Be(70m);
    }

    [Fact]
    public async Task Le_lancement_est_bloque_si_une_matiere_manque()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync(stockArgile: 22m);
        var ordre = await CreerProductionAsync(produitId, 20m);

        var action = async () => await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());

        var exception = await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Matières insuffisantes*");

        // Le message indique la matière concernée, le nécessaire et le disponible.
        exception.Which.Details.Should().ContainSingle()
            .Which.Should().Contain("Argile").And.Contain("30").And.Contain("22");
    }

    [Fact]
    public async Task Un_lancement_bloque_ne_touche_pas_au_stock()
    {
        var (produitId, _, argileId) = await _atelier.PreparerVaseAsync(stockArgile: 22m);
        var ordre = await CreerProductionAsync(produitId, 20m);

        try
        {
            await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());
        }
        catch (BusinessRuleException)
        {
            // Attendu : la vérification précède toute consommation.
        }

        (await _atelier.StockMatiereAsync(argileId)).Should().Be(22m);
    }

    [Fact]
    public async Task Une_derogation_administrateur_permet_de_lancer_malgre_le_manque()
    {
        var (produitId, _, argileId) = await _atelier.PreparerVaseAsync(stockArgile: 22m);
        var ordre = await CreerProductionAsync(produitId, 20m);
        _atelier.UtilisateurCourant.Droits.Add(PermissionCodes.ProductionDeroger);

        var lance = await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete
        {
            ForcerMalgreStockInsuffisant = true,
            MotifDerogation = "Livraison du fournisseur attendue dans la journée"
        });

        lance.DerogationStock.Should().BeTrue();
        lance.MotifDerogation.Should().Contain("Livraison");
        (await _atelier.StockMatiereAsync(argileId)).Should().Be(-8m);
        _atelier.Audit.Traces.Should().Contain(t => t.Action == AuditAction.Derogation);
    }

    [Fact]
    public async Task Une_derogation_sans_le_droit_correspondant_est_refusee()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync(stockArgile: 10m);
        var ordre = await CreerProductionAsync(produitId, 20m);

        var action = async () => await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete
        {
            ForcerMalgreStockInsuffisant = true,
            MotifDerogation = "Je force"
        });

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Matières insuffisantes*");
    }

    [Fact]
    public async Task Les_etapes_avancent_dans_l_ordre_et_sont_historisees()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await CreerProductionAsync(produitId, 20m);
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());

        await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.Faconnage });
        var apres = await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.Sechage });

        apres.Statut.Should().Be(ProductionStatus.Sechage);
        apres.Etapes.Select(e => e.Etape).Should().ContainInOrder(
            ProductionStatus.Preparation, ProductionStatus.Faconnage, ProductionStatus.Sechage);
    }

    [Fact]
    public async Task Une_production_ne_peut_pas_revenir_en_arriere()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await CreerProductionAsync(produitId, 20m);
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());
        await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.Sechage });

        var action = async () => await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.Faconnage });

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*étape suivante*");
    }

    [Fact]
    public async Task Une_production_non_lancee_ne_peut_pas_avancer()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await CreerProductionAsync(produitId, 20m);

        var action = async () => await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.Faconnage });

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Lancez d'abord*");
    }

    [Fact]
    public async Task Terminer_sans_controle_qualite_est_refuse()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await CreerProductionAsync(produitId, 20m);
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());
        await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.ControleQualite });

        var action = async () => await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.Termine, QuantiteAcceptee = 20m });

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*contrôle qualité est obligatoire*");
    }

    [Fact]
    public async Task Terminer_avec_un_controle_non_conforme_est_refuse()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await CreerProductionAsync(produitId, 20m);
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());
        await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.ControleQualite });

        await _atelier.Qualite.EnregistrerAsync(new ControleQualiteRequete
        {
            ProductionId = ordre.Id, QuantiteControlee = 20m, QuantiteRefusee = 20m
        });

        var action = async () => await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.Termine });

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*non conforme*");
    }

    [Fact]
    public async Task Une_production_terminee_alimente_le_stock_des_produits_finis()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await CreerProductionAsync(produitId, 20m);
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());
        await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.ControleQualite });
        await _atelier.ControlerAsync(ordre.Id, 20m);

        var termine = await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.Termine, QuantiteAcceptee = 20m });

        termine.Statut.Should().Be(ProductionStatus.Termine);
        termine.QuantiteTerminee.Should().Be(20m);
        (await _atelier.StockProduitAsync(produitId)).Should().Be(20m);
    }

    [Fact]
    public async Task Le_cout_de_revient_reel_est_reporte_sur_la_fiche_produit()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await CreerProductionAsync(produitId, 20m);
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());
        await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.ControleQualite });
        await _atelier.ControlerAsync(ordre.Id, 20m);
        await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.Termine, QuantiteAcceptee = 20m });

        // Matières 11 000 DA + main-d'œuvre 12 000 DA + emballage 1 000 DA = 24 000 DA pour 20 pièces.
        var produit = await _atelier.Contexte.Products.AsNoTracking().FirstAsync(p => p.Id == produitId);
        produit.ProductionCost.Should().Be(1200m);
    }

    [Fact]
    public async Task Les_pieces_endommagees_sont_comptees_a_part()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await CreerProductionAsync(produitId, 20m);
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());

        var apres = await _atelier.Production.ChangerEtapeAsync(ordre.Id, new ChangementEtapeRequete
        {
            NouvelleEtape = ProductionStatus.Sechage, QuantiteAcceptee = 18m, QuantiteEndommagee = 2m
        });

        apres.QuantiteEndommagee.Should().Be(2m);
    }

    [Fact]
    public async Task L_annulation_remet_les_matieres_en_stock()
    {
        var (produitId, _, argileId) = await _atelier.PreparerVaseAsync(stockArgile: 100m);
        var ordre = await CreerProductionAsync(produitId, 20m);
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());

        var annule = await _atelier.Production.AnnulerAsync(ordre.Id, "Commande annulée par le client");

        annule.Statut.Should().Be(ProductionStatus.Annule);
        (await _atelier.StockMatiereAsync(argileId)).Should().Be(100m);
    }

    [Fact]
    public async Task Une_production_terminee_ne_peut_plus_etre_annulee()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await CreerProductionAsync(produitId, 20m);
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());
        await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.ControleQualite });
        await _atelier.ControlerAsync(ordre.Id, 20m);
        await _atelier.Production.ChangerEtapeAsync(ordre.Id,
            new ChangementEtapeRequete { NouvelleEtape = ProductionStatus.Termine, QuantiteAcceptee = 20m });

        var action = async () => await _atelier.Production.AnnulerAsync(ordre.Id, "Trop tard");

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*terminée*");
    }

    [Fact]
    public async Task Le_tableau_de_production_regroupe_les_ordres_par_etape()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var premier = await CreerProductionAsync(produitId, 5m);
        var second = await CreerProductionAsync(produitId, 5m);
        await _atelier.Production.LancerAsync(second.Id, new LancementProductionRequete());

        var tableau = await _atelier.Production.TableauAsync();

        tableau.Single(c => c.Etape == ProductionStatus.Planifie).Ordres
            .Should().ContainSingle(o => o.Id == premier.Id);
        tableau.Single(c => c.Etape == ProductionStatus.Preparation).Ordres
            .Should().ContainSingle(o => o.Id == second.Id);
    }

    public void Dispose() => _atelier.Dispose();
}
