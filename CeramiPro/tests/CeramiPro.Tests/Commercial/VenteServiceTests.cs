using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Domain.Enums;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Tests.Commercial;

public class VenteServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    public void Dispose() => _atelier.Dispose();

    [Fact]
    public async Task Une_vente_diminue_le_stock_et_recoit_un_numero()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m, stockInitial: 10m);

        var vente = await _atelier.VendreAsync(produitId, quantite: 3m, prix: 3500m,
            clientId: await _atelier.CreerClientAsync());

        vente.Numero.Should().StartWith("VTE-");
        vente.Total.Should().Be(10500m);
        vente.Statut.Should().Be(SaleStatus.Confirmee);
        (await _atelier.StockProduitAsync(produitId)).Should().Be(7m);
    }

    [Fact]
    public async Task Le_prix_du_catalogue_est_repris_quand_aucun_prix_n_est_saisi()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m, stockInitial: 10m);

        var vente = await _atelier.VendreAsync(produitId, quantite: 2m, prix: 0m);

        vente.Total.Should().Be(7000m);
    }

    [Fact]
    public async Task Le_benefice_est_calcule_a_partir_du_cout_de_revient()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m,
            coutProduction: 1850m, stockInitial: 10m);

        var vente = await _atelier.VendreAsync(produitId, prix: 3500m);

        vente.CoutRevient.Should().Be(1850m);
        vente.Benefice.Should().Be(1650m);
    }

    [Fact]
    public async Task Une_remise_est_deduite_du_total()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 1000m, stockInitial: 10m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Remise = 500m,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 5m } }
        });

        vente.SousTotal.Should().Be(5000m);
        vente.Total.Should().Be(4500m);
    }

    [Fact]
    public async Task Une_vente_sans_produit_est_refusee()
    {
        var action = () => _atelier.Ventes.EnregistrerAsync(new VenteRequete());

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*au moins un produit*");
    }

    [Fact]
    public async Task Vendre_plus_que_le_stock_disponible_est_refuse()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 2m);

        var action = () => _atelier.VendreAsync(produitId, quantite: 5m);

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*Stock insuffisant*");
    }

    [Fact]
    public async Task Une_vente_refusee_ne_laisse_aucune_trace_dans_le_stock()
    {
        var vase = await _atelier.CreerProduitAsync("Vase", stockInitial: 10m);
        var bol = await _atelier.CreerProduitAsync("Bol", stockInitial: 1m);

        // Le vase est en stock, le bol non : toute la vente est refusée.
        var action = () => _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Lignes = new List<LigneVenteRequete>
            {
                new() { ProduitId = vase, Quantite = 2m },
                new() { ProduitId = bol, Quantite = 3m }
            }
        });

        await action.Should().ThrowAsync<RegleMetierException>();

        // Une opération suivante ne doit pas enregistrer en silence le stock
        // du vase, diminué en mémoire avant le refus.
        await _atelier.CreerClientAsync("Client suivant");

        (await _atelier.StockProduitAsync(vase)).Should().Be(10m);
        (await _atelier.StockProduitAsync(bol)).Should().Be(1m);
    }

    [Fact]
    public async Task Un_reglement_immediat_est_encaisse_avec_la_vente()
    {
        var clientId = await _atelier.CreerClientAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 1000m, stockInitial: 10m);

        var vente = await _atelier.VendreAsync(produitId, quantite: 5m, prix: 1000m, paye: 3000m, clientId: clientId);

        vente.Paye.Should().Be(3000m);
        vente.Reste.Should().Be(2000m);

        var paiements = await _atelier.Paiements.ListerAsync(new FiltrePaiementsRequete { VenteId = vente.Id });
        paiements.Elements.Should().ContainSingle().Which.ClientId.Should().Be(clientId);
    }

    [Fact]
    public async Task Encaisser_plus_que_le_total_est_refuse()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 1000m, stockInitial: 10m);

        var action = () => _atelier.VendreAsync(produitId, prix: 1000m, paye: 1500m);

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*dépasse le total*");
        (await _atelier.StockProduitAsync(produitId)).Should().Be(10m);
    }

    [Fact]
    public async Task Un_encaissement_sans_mode_de_reglement_est_refuse_avant_toute_ecriture()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 1000m, stockInitial: 10m);

        var action = () => _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            MontantPaye = 1000m,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*mode de règlement*");
        (await _atelier.Contexte.Sales.CountAsync()).Should().Be(0);
        (await _atelier.StockProduitAsync(produitId)).Should().Be(10m);
    }

    [Fact]
    public async Task Une_vente_annulee_remet_les_produits_en_stock_et_garde_le_motif()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 10m);
        var vente = await _atelier.VendreAsync(produitId, quantite: 4m);

        var annulee = await _atelier.Ventes.AnnulerAsync(vente.Id, "Erreur de saisie");

        annulee.Statut.Should().Be(SaleStatus.Annulee);
        (await _atelier.StockProduitAsync(produitId)).Should().Be(10m);
        _atelier.Audit.Traces.Should().Contain(t => t.Description!.Contains("Erreur de saisie"));
    }

    [Fact]
    public async Task Une_vente_ne_s_annule_pas_deux_fois()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 10m);
        var vente = await _atelier.VendreAsync(produitId, quantite: 4m);
        await _atelier.Ventes.AnnulerAsync(vente.Id, "Erreur");

        var action = () => _atelier.Ventes.AnnulerAsync(vente.Id, "Encore");

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*déjà annulée*");
        (await _atelier.StockProduitAsync(produitId)).Should().Be(10m, "le stock ne revient qu'une fois");
    }

    [Fact]
    public async Task Une_vente_reglee_ne_s_annule_qu_en_remboursant()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 1000m, stockInitial: 10m);
        var vente = await _atelier.VendreAsync(produitId, prix: 1000m, paye: 1000m);

        var sansRemboursement = () => _atelier.Ventes.AnnulerAsync(vente.Id, "Retour");
        await sansRemboursement.Should().ThrowAsync<RegleMetierException>().WithMessage("*rembourser*");

        var annulee = await _atelier.Ventes.AnnulerAsync(vente.Id, "Retour", rembourser: true);

        annulee.Statut.Should().Be(SaleStatus.Annulee);
        annulee.Paye.Should().Be(0m);
        (await _atelier.StockProduitAsync(produitId)).Should().Be(10m);
    }

    [Fact]
    public async Task Une_vente_annulee_ne_compte_plus_dans_les_ventes_du_jour()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 1000m, stockInitial: 10m);
        var vente = await _atelier.VendreAsync(produitId, prix: 1000m);
        await _atelier.VendreAsync(produitId, prix: 1000m);

        await _atelier.Ventes.AnnulerAsync(vente.Id, "Erreur");

        var tableau = await _atelier.TableauDeBord.ObtenirAsync();
        tableau.Aujourdhui.ChiffreAffaires.Should().Be(1000m);
        tableau.Aujourdhui.NombreVentes.Should().Be(1);
    }

    [Fact]
    public async Task L_historique_ne_garde_que_les_ventes_a_credit_sur_demande()
    {
        var clientId = await _atelier.CreerClientAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 1000m, stockInitial: 10m);
        await _atelier.VendreAsync(produitId, prix: 1000m, paye: 1000m);
        var aCredit = await _atelier.VendreAsync(produitId, prix: 1000m, paye: 200m, clientId: clientId);

        var impayees = await _atelier.Ventes.ListerAsync(new FiltreVentesRequete { SeulementImpayees = true });

        impayees.Elements.Should().ContainSingle().Which.Id.Should().Be(aCredit.Id);
    }

    [Fact]
    public async Task La_date_d_une_vente_est_celle_de_l_atelier()
    {
        // 23 h 30 à Alger, 22 h 30 en temps universel : la vente est du jour même.
        _atelier.Horloge.MaintenantUtc = new DateTime(2026, 8, 31, 22, 30, 0, DateTimeKind.Utc);
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 10m);

        var vente = await _atelier.VendreAsync(produitId);

        vente.Date.Should().Be(new DateTime(2026, 8, 31, 23, 30, 0));
    }
}
