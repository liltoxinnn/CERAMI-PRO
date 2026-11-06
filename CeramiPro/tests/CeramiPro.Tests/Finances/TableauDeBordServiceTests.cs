using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Services;
using CeramiPro.Tests.Aides;
using FluentAssertions;

namespace CeramiPro.Tests.Finances;

public class TableauDeBordServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    [Fact]
    public async Task Le_tableau_de_bord_d_un_atelier_vide_n_affiche_que_des_zeros()
    {
        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.Aujourdhui.ChiffreAffaires.Should().Be(0m);
        tableau.Aujourdhui.NombreVentes.Should().Be(0);
        tableau.Mois.Resultat.Should().Be(0m);
        tableau.Stock.ValeurStock.Should().Be(0m);
        tableau.ProduitsLesPlusVendus.Should().BeEmpty();
    }

    [Fact]
    public async Task Une_vente_du_jour_alimente_le_chiffre_d_affaires_et_le_benefice()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m,
            coutProduction: 1850m, stockInitial: 10m);

        await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 2m } }
        });

        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.Aujourdhui.NombreVentes.Should().Be(1);
        tableau.Aujourdhui.ChiffreAffaires.Should().Be(7000m);
        tableau.Mois.ChiffreAffaires.Should().Be(7000m);
        tableau.Mois.Benefice.Should().Be(3300m);
    }

    [Fact]
    public async Task Les_depenses_du_mois_sont_deduites_du_resultat()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m,
            coutProduction: 1850m, stockInitial: 10m);

        await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 2m } }
        });

        await _atelier.CreerDepenseAsync(1300m);

        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.Mois.Depenses.Should().Be(1300m);
        tableau.Mois.Resultat.Should().Be(3300m - 1300m);
    }

    [Fact]
    public async Task La_valeur_du_stock_est_calculee_au_cout_des_pieces()
    {
        await _atelier.CreerProduitAsync("Vase", coutProduction: 1850m, stockInitial: 4m);
        await _atelier.CreerProduitAsync("Bol", coutProduction: 300m, stockInitial: 2m);

        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.Stock.NombreProduits.Should().Be(2);
        tableau.Stock.ValeurStock.Should().Be(8000m);
    }

    [Fact]
    public async Task Un_produit_sous_son_seuil_apparait_dans_les_alertes()
    {
        await _atelier.CreerProduitAsync("Vase", stockInitial: 1m, stockMinimum: 3m);
        await _atelier.CreerProduitAsync("Bol", stockInitial: 10m, stockMinimum: 3m);
        await _atelier.CreerProduitAsync("Jarre signée", stockInitial: 0m, pieceUnique: true);

        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        // Une pièce unique vendue ne se refabrique pas : pas d'alerte.
        tableau.Stock.ProduitsFaibles.Should().Be(1);
        tableau.StockFaible.Should().ContainSingle().Which.Nom.Should().Be("Vase");
    }

    [Fact]
    public async Task Les_commandes_en_retard_et_pretes_sont_comptees()
    {
        var clientId = await _atelier.CreerClientAsync();
        var enRetard = await _atelier.CommanderAsync(clientId);
        var prete = await _atelier.CommanderAsync(clientId);
        await _atelier.Commandes.ChangerStatutAsync(prete.Id, Domain.Enums.CustomOrderStatus.Prete);

        _atelier.Horloge.Avancer(TimeSpan.FromDays(11));

        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.Commandes.EnRetard.Should().BeGreaterThanOrEqualTo(1);
        tableau.Commandes.PretesALivrer.Should().Be(1);
        enRetard.Id.Should().NotBe(prete.Id);
    }

    [Fact]
    public async Task Le_reste_a_encaisser_correspond_aux_ventes_a_credit()
    {
        var clientId = await _atelier.CreerClientAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m, stockInitial: 10m);

        await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            MontantPaye = 1000m,
            ModeReglementId = _atelier.ModeReglementId,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.Aujourdhui.Encaisse.Should().Be(1000m);
        tableau.Finances.ResteAEncaisser.Should().Be(2500m);
    }

    [Fact]
    public async Task Les_graphiques_couvrent_les_periodes_annoncees()
    {
        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.VentesParJour.Should().HaveCount(TableauDeBordService.JoursGraphique);
        tableau.VentesParMois.Should().HaveCount(TableauDeBordService.MoisGraphique);
        tableau.VentesParJour.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Etiquette));
    }

    [Fact]
    public async Task Le_classement_des_produits_les_plus_vendus_suit_les_quantites()
    {
        var vase = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m, stockInitial: 20m);
        var assiette = await _atelier.CreerProduitAsync("Assiette", prixVente: 900m, stockInitial: 20m);

        await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Lignes = new List<LigneVenteRequete>
            {
                new() { ProduitId = vase, Quantite = 2m },
                new() { ProduitId = assiette, Quantite = 9m }
            }
        });

        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.ProduitsLesPlusVendus.Should().HaveCount(2);
        tableau.ProduitsLesPlusVendus[0].Nom.Should().Be("Assiette");
        tableau.ProduitsLesPlusVendus[0].Quantite.Should().Be(9m);
    }

    public void Dispose() => _atelier.Dispose();
}
