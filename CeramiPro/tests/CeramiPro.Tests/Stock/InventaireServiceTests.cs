using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Services;
using CeramiPro.Domain.Entities.Inventory;
using CeramiPro.Domain.Entities.Settings;
using CeramiPro.Domain.Enums;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Tests.Stock;

/// <summary>
/// Le stock ne change jamais sans laisser de trace : chaque entrée, casse,
/// vente ou correction est un mouvement daté, avec le stock avant et après.
/// </summary>
public class InventaireServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    public void Dispose() => _atelier.Dispose();

    private Task AutoriserStockNegatifAsync()
    {
        _atelier.Contexte.SystemSettings.Add(new SystemSetting
        {
            Key = InventaireService.CleStockNegatif, Value = "true", ValueType = "booleen"
        });
        return _atelier.Contexte.SaveChangesAsync();
    }

    private async Task<InventoryTransaction> DernierMouvementAsync()
        => await _atelier.Contexte.InventoryTransactions.AsNoTracking().OrderByDescending(t => t.Id).FirstAsync();

    // ------------------------------------------------------------ Mouvements

    [Fact]
    public async Task Un_mouvement_conserve_le_stock_avant_et_apres()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 30m);

        await _atelier.Inventaire.EnregistrerAsync(new MouvementStockRequete
        {
            TypeMouvement = InventoryTransactionType.Perte, ProduitId = produitId, Quantite = -12m
        });
        await _atelier.Contexte.SaveChangesAsync();

        var mouvement = await DernierMouvementAsync();
        mouvement.QuantityBefore.Should().Be(30m);
        mouvement.QuantityAfter.Should().Be(18m);
        (await _atelier.StockProduitAsync(produitId)).Should().Be(18m);
    }

    [Fact]
    public async Task Le_stock_initial_d_un_produit_laisse_un_mouvement_d_entree()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 6m);

        var historique = await _atelier.Inventaire.ListerAsync(new FiltreMouvementsRequete { ProduitId = produitId });

        historique.Elements.Should().ContainSingle();
        historique.Elements[0].Quantite.Should().Be(6m);
        historique.Elements[0].StockApres.Should().Be(6m);
    }

    [Fact]
    public async Task Le_stock_ne_peut_pas_devenir_negatif()
    {
        var produitId = await _atelier.CreerProduitAsync("Bol", stockInitial: 2m);

        var action = () => _atelier.Inventaire.EnregistrerAsync(new MouvementStockRequete
        {
            TypeMouvement = InventoryTransactionType.Vente, ProduitId = produitId, Quantite = -7m
        });

        var exception = await action.Should().ThrowAsync<RegleMetierException>();
        exception.Which.Message.Should().Contain("Stock insuffisant").And.Contain("Bol")
            .And.Contain("2").And.Contain("7");
    }

    [Fact]
    public async Task Le_reglage_de_l_atelier_peut_autoriser_le_stock_negatif()
    {
        var produitId = await _atelier.CreerProduitAsync("Bol", stockInitial: 1m);
        await AutoriserStockNegatifAsync();

        await _atelier.Inventaire.EnregistrerAsync(new MouvementStockRequete
        {
            TypeMouvement = InventoryTransactionType.Vente, ProduitId = produitId, Quantite = -3m
        });
        await _atelier.Contexte.SaveChangesAsync();

        (await _atelier.StockProduitAsync(produitId)).Should().Be(-2m);
    }

    [Fact]
    public async Task Un_mouvement_nul_est_refuse()
    {
        var produitId = await _atelier.CreerProduitAsync("Bol", stockInitial: 1m);

        var action = () => _atelier.Inventaire.EnregistrerAsync(new MouvementStockRequete
        {
            TypeMouvement = InventoryTransactionType.Entree, ProduitId = produitId, Quantite = 0m
        });

        await action.Should().ThrowAsync<RegleMetierException>();
    }

    [Fact]
    public async Task Une_entree_a_un_autre_cout_fait_la_moyenne_ponderee()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", coutProduction: 1000m, stockInitial: 4m);

        await _atelier.Inventaire.EnregistrerManuelAsync(new MouvementManuelRequete
        {
            ProduitId = produitId, TypeMouvement = InventoryTransactionType.Entree,
            Quantite = 4m, CoutUnitaire = 2000m
        });

        // 4 pièces à 1 000 + 4 pièces à 2 000 : 1 500 la pièce.
        (await _atelier.Produits.ObtenirAsync(produitId)).CoutProduction.Should().Be(1500m);
    }

    // ------------------------------------------------------------ Pièces uniques

    [Fact]
    public async Task Une_piece_unique_ne_compte_jamais_plus_d_un_exemplaire()
    {
        var piece = await _atelier.CreerProduitAsync("Jarre signée", stockInitial: 1m, pieceUnique: true);

        var action = () => _atelier.Inventaire.EnregistrerManuelAsync(new MouvementManuelRequete
        {
            ProduitId = piece, TypeMouvement = InventoryTransactionType.Entree, Quantite = 1m
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*pièce unique*");
        (await _atelier.StockProduitAsync(piece)).Should().Be(1m);
    }

    [Fact]
    public async Task Une_piece_unique_ne_se_vend_pas_deux_fois_meme_si_le_stock_negatif_est_permis()
    {
        var piece = await _atelier.CreerProduitAsync("Jarre signée", stockInitial: 1m, pieceUnique: true);
        await AutoriserStockNegatifAsync();

        await _atelier.VendreAsync(piece, prix: 25000m);

        var action = () => _atelier.VendreAsync(piece, prix: 25000m);

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*n'est plus disponible*");
        (await _atelier.StockProduitAsync(piece)).Should().Be(0m);
    }

    [Fact]
    public async Task Une_piece_unique_ne_se_cree_pas_avec_plusieurs_exemplaires()
    {
        var action = () => _atelier.CreerProduitAsync("Jarre signée", stockInitial: 3m, pieceUnique: true);

        await action.Should().ThrowAsync<RegleMetierException>();
    }

    [Fact]
    public async Task Une_piece_unique_vendue_revient_en_stock_si_la_vente_est_annulee()
    {
        var piece = await _atelier.CreerProduitAsync("Jarre signée", stockInitial: 1m, pieceUnique: true);
        var vente = await _atelier.VendreAsync(piece, prix: 25000m);

        await _atelier.Ventes.AnnulerAsync(vente.Id, "Erreur de saisie");

        (await _atelier.StockProduitAsync(piece)).Should().Be(1m);
    }

    // ------------------------------------------------------------ Saisie manuelle

    [Fact]
    public async Task Une_casse_retire_les_pieces_et_garde_le_motif()
    {
        var produitId = await _atelier.CreerProduitAsync("Assiette", stockInitial: 10m);

        var mouvement = await _atelier.Inventaire.EnregistrerManuelAsync(new MouvementManuelRequete
        {
            ProduitId = produitId, TypeMouvement = InventoryTransactionType.Perte,
            Quantite = 3m, Notes = "Fêlées à la cuisson"
        });

        mouvement.Quantite.Should().Be(-3m, "une casse est une sortie, saisie en nombre positif");
        mouvement.Notes.Should().Contain("Fêlées");
        (await _atelier.StockProduitAsync(produitId)).Should().Be(7m);
    }

    [Fact]
    public async Task Une_casse_doit_etre_expliquee()
    {
        var produitId = await _atelier.CreerProduitAsync("Assiette", stockInitial: 10m);

        var action = () => _atelier.Inventaire.EnregistrerManuelAsync(new MouvementManuelRequete
        {
            ProduitId = produitId, TypeMouvement = InventoryTransactionType.Perte, Quantite = 1m
        });

        await action.Should().ThrowAsync<RegleMetierException>();
        (await _atelier.StockProduitAsync(produitId)).Should().Be(10m);
    }

    [Fact]
    public async Task Une_quantite_negative_saisie_a_la_main_est_refusee()
    {
        var produitId = await _atelier.CreerProduitAsync("Assiette", stockInitial: 10m);

        var action = () => _atelier.Inventaire.EnregistrerManuelAsync(new MouvementManuelRequete
        {
            ProduitId = produitId, TypeMouvement = InventoryTransactionType.Entree, Quantite = -4m
        });

        await action.Should().ThrowAsync<RegleMetierException>();
    }

    [Fact]
    public async Task Seules_les_entrees_et_les_casses_se_saisissent_a_la_main()
    {
        var produitId = await _atelier.CreerProduitAsync("Assiette", stockInitial: 10m);

        // Une vente se saisit à la caisse : la saisir ici créerait une sortie
        // sans vente, donc sans recette.
        var action = () => _atelier.Inventaire.EnregistrerManuelAsync(new MouvementManuelRequete
        {
            ProduitId = produitId, TypeMouvement = InventoryTransactionType.Vente, Quantite = 1m
        });

        await action.Should().ThrowAsync<RegleMetierException>();
    }

    [Fact]
    public async Task Corriger_apres_comptage_enregistre_seulement_l_ecart()
    {
        var produitId = await _atelier.CreerProduitAsync("Tasse", stockInitial: 12m);

        var mouvement = await _atelier.Inventaire.RegulariserAsync(new RegularisationRequete
        {
            ProduitId = produitId, QuantiteComptee = 9m, Motif = "Comptage de fin de mois"
        });

        mouvement.Quantite.Should().Be(-3m);
        mouvement.StockAvant.Should().Be(12m);
        mouvement.StockApres.Should().Be(9m);
        (await _atelier.StockProduitAsync(produitId)).Should().Be(9m);
    }

    [Fact]
    public async Task Une_correction_sans_ecart_est_refusee()
    {
        var produitId = await _atelier.CreerProduitAsync("Tasse", stockInitial: 12m);

        var action = () => _atelier.Inventaire.RegulariserAsync(new RegularisationRequete
        {
            ProduitId = produitId, QuantiteComptee = 12m, Motif = "Comptage"
        });

        await action.Should().ThrowAsync<RegleMetierException>();
    }

    [Fact]
    public async Task Une_correction_sans_motif_est_refusee()
    {
        var produitId = await _atelier.CreerProduitAsync("Tasse", stockInitial: 12m);

        var action = () => _atelier.Inventaire.RegulariserAsync(new RegularisationRequete
        {
            ProduitId = produitId, QuantiteComptee = 10m
        });

        await action.Should().ThrowAsync<RegleMetierException>();
    }

    // ------------------------------------------------------------ Historique

    [Fact]
    public async Task Une_vente_puis_son_annulation_laissent_deux_mouvements()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 10m);
        var vente = await _atelier.VendreAsync(produitId, quantite: 2m);

        await _atelier.Ventes.AnnulerAsync(vente.Id, "Client parti");

        var historique = await _atelier.Inventaire.ListerAsync(new FiltreMouvementsRequete { ProduitId = produitId });

        // L'entrée de départ, la vente, puis son annulation : rien n'est effacé.
        historique.Elements.Should().HaveCount(3);
        historique.Elements.Select(m => m.Quantite).Should().BeEquivalentTo(new[] { 10m, -2m, 2m });
        (await _atelier.StockProduitAsync(produitId)).Should().Be(10m);
    }

    [Fact]
    public async Task L_historique_se_filtre_par_type_de_mouvement()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 10m);
        await _atelier.VendreAsync(produitId, quantite: 2m);

        var ventes = await _atelier.Inventaire.ListerAsync(new FiltreMouvementsRequete
        {
            ProduitId = produitId, TypeMouvement = InventoryTransactionType.Vente
        });

        ventes.Elements.Should().ContainSingle().Which.Quantite.Should().Be(-2m);
    }

    [Fact]
    public async Task Chaque_mouvement_note_qui_l_a_fait()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 10m);

        var historique = await _atelier.Inventaire.ListerAsync(new FiltreMouvementsRequete { ProduitId = produitId });

        historique.Elements[0].Utilisateur.Should().NotBeNullOrWhiteSpace();
    }
}
