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
        tableau.Stock.ValeurTotale.Should().Be(0m);
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
        tableau.Aujourdhui.Benefice.Should().Be(3300m);
        tableau.Mois.ChiffreAffaires.Should().Be(7000m);
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
        tableau.Finances.DepensesMois.Should().Be(1300m);
    }

    [Fact]
    public async Task La_valeur_du_stock_additionne_les_matieres_et_les_produits()
    {
        await _atelier.CreerMatiereAsync("Argile", stockInitial: 100m, prix: 200m);
        await _atelier.CreerProduitAsync("Vase", coutProduction: 1850m, stockInitial: 4m);

        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.Stock.ValeurMatieres.Should().Be(20000m);
        tableau.Stock.ValeurProduits.Should().Be(7400m);
        tableau.Stock.ValeurTotale.Should().Be(27400m);
    }

    [Fact]
    public async Task Une_matiere_sous_son_seuil_est_comptee_dans_les_alertes()
    {
        await _atelier.CreerMatiereAsync("Émail", stockInitial: 2m, stockMinimum: 5m);
        await _atelier.CreerProduitAsync("Vase", stockInitial: 1m, stockMinimum: 3m);

        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.Stock.MatieresFaibles.Should().Be(1);
        tableau.Stock.ProduitsFaibles.Should().Be(1);
    }

    [Fact]
    public async Task Les_creances_clients_correspondent_au_reste_a_payer()
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

        tableau.Finances.ArgentRecu.Should().Be(1000m);
        tableau.Finances.CreancesClients.Should().Be(2500m);
    }

    [Fact]
    public async Task Les_graphiques_couvrent_les_periodes_annoncees()
    {
        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.VentesParJour.Should().HaveCount(TableauDeBordService.JoursGraphique);
        tableau.VentesParMois.Should().HaveCount(TableauDeBordService.MoisGraphique);
        tableau.BeneficesParMois.Should().HaveCount(TableauDeBordService.MoisGraphique);
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
