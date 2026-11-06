using System.Text;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Domain.Enums;
using CeramiPro.Tests.Aides;
using FluentAssertions;

namespace CeramiPro.Tests.Finances;

public class RapportServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    private RapportRequete Mois(TypeRapport type) => new()
    {
        Type = type,
        Du = new DateTime(_atelier.Horloge.Aujourdhui.Year, _atelier.Horloge.Aujourdhui.Month, 1),
        Au = _atelier.Horloge.Aujourdhui
    };

    [Theory]
    [InlineData(TypeRapport.VentesParJour)]
    [InlineData(TypeRapport.VentesParMois)]
    [InlineData(TypeRapport.Benefices)]
    [InlineData(TypeRapport.Depenses)]
    [InlineData(TypeRapport.DettesClients)]
    [InlineData(TypeRapport.ProduitsLesPlusVendus)]
    [InlineData(TypeRapport.ProduitsLesPlusRentables)]
    [InlineData(TypeRapport.ValeurStock)]
    public async Task Chaque_rapport_repond_avec_un_titre_et_des_colonnes(TypeRapport type)
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 5m);
        await _atelier.VendreAsync(produitId, paye: 1000m, clientId: await _atelier.CreerClientAsync());
        await _atelier.CreerDepenseAsync(1200m);

        var rapport = await _atelier.Rapports.GenererAsync(Mois(type));

        rapport.Type.Should().Be(type);
        rapport.Titre.Should().NotBeNullOrWhiteSpace();
        rapport.Periode.Should().NotBeNullOrWhiteSpace();
        rapport.Colonnes.Should().NotBeEmpty();
        rapport.Lignes.All(l => l.Count == rapport.Colonnes.Count).Should().BeTrue();
        if (rapport.Totaux is not null)
        {
            rapport.Totaux.Should().HaveCount(rapport.Colonnes.Count);
        }
    }

    [Fact]
    public async Task Les_ventes_du_jour_totalisent_les_ventes_confirmees()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m, stockInitial: 10m);
        await _atelier.VendreAsync(produitId, quantite: 2m, prix: 3500m);

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.VentesParJour));

        rapport.Lignes.Should().ContainSingle();
        rapport.Lignes[0][1].Should().Be("1");
        rapport.Totaux.Should().NotBeNull();
        rapport.Totaux![4].Should().Contain("7").And.Contain("000").And.Contain("DA");
        rapport.Graphique.Should().NotBeNull();
        rapport.Graphique!.Sum(p => p.Valeur).Should().Be(7000m);
    }

    [Fact]
    public async Task Le_chiffre_d_affaires_compte_aussi_les_commandes_livrees()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m, stockInitial: 10m);
        await _atelier.VendreAsync(produitId, prix: 3500m);

        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync(), prixUnitaire: 12000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 4000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 8000m, acompte: false);
        await _atelier.Commandes.ChangerStatutAsync(commande.Id, CustomOrderStatus.Livree);

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.VentesParJour));

        rapport.Totaux![3].Should().StartWith("1");
        rapport.Graphique!.Sum(p => p.Valeur).Should().Be(15500m);
    }

    [Fact]
    public async Task Une_commande_non_livree_n_entre_pas_dans_le_chiffre_d_affaires()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync(), prixUnitaire: 12000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 4000m);

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.VentesParJour));

        rapport.Graphique!.Sum(p => p.Valeur).Should().Be(0m);
    }

    [Fact]
    public async Task Les_ventes_par_mois_couvrent_chaque_mois_de_la_periode()
    {
        var rapport = await _atelier.Rapports.GenererAsync(new RapportRequete
        {
            Type = TypeRapport.VentesParMois,
            Du = new DateTime(2026, 1, 1),
            Au = new DateTime(2026, 8, 31)
        });

        rapport.Lignes.Should().HaveCount(8);
        rapport.Lignes[0][0].Should().Be("Janvier 2026");
        rapport.Lignes[7][0].Should().Be("Août 2026");
    }

    [Fact]
    public async Task Le_benefice_retire_le_cout_des_pieces_et_les_depenses()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m,
            coutProduction: 1500m, stockInitial: 10m);
        await _atelier.VendreAsync(produitId, quantite: 2m, prix: 3500m);
        await _atelier.CreerDepenseAsync(1000m);

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.Benefices));

        // 7 000 de ventes − 3 000 de coût des pièces − 1 000 de dépenses.
        rapport.Totaux![^1].Should().Contain("3").And.Contain("000");
        rapport.Totaux[^1].Should().NotStartWith("-");
    }

    [Fact]
    public async Task Le_rapport_des_depenses_regroupe_par_categorie()
    {
        await _atelier.CreerDepenseAsync(3000m);
        await _atelier.CreerDepenseAsync(2000m);

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.Depenses));

        rapport.Colonnes.Should().BeEquivalentTo(new[] { "Catégorie", "Nombre", "Montant" });
        rapport.Lignes.Should().ContainSingle();
        rapport.Lignes[0][0].Should().Be("Électricité");
        rapport.Lignes[0][1].Should().Be("2");
        rapport.Totaux![2].Should().Contain("5");
    }

    [Fact]
    public async Task Le_rapport_des_dettes_clients_liste_le_reste_a_payer()
    {
        var clientId = await _atelier.CreerClientAsync("Karim Saïdi");
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m, stockInitial: 10m);
        await _atelier.VendreAsync(produitId, prix: 3500m, paye: 500m, clientId: clientId);

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.DettesClients));

        rapport.Lignes.Should().ContainSingle();
        rapport.Lignes[0][0].Should().Be("Karim Saïdi");
        rapport.Lignes[0][4].Should().Contain("3").And.Contain("000");
    }

    [Fact]
    public async Task Le_rapport_des_dettes_tient_compte_des_acomptes_des_commandes()
    {
        var clientId = await _atelier.CreerClientAsync("Nadia Amrani");
        var commande = await _atelier.CommanderAsync(clientId, prixUnitaire: 12000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 4000m);

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.DettesClients));

        rapport.Lignes.Should().ContainSingle(l => l[0] == "Nadia Amrani");
        rapport.Lignes.Single(l => l[0] == "Nadia Amrani")[4].Should().Contain("8").And.Contain("000");
    }

    [Fact]
    public async Task Les_produits_les_plus_vendus_sont_classes_par_quantite()
    {
        var vase = await _atelier.CreerProduitAsync("Vase", stockInitial: 10m);
        var tasse = await _atelier.CreerProduitAsync("Tasse", prixVente: 800m, stockInitial: 10m);
        await _atelier.VendreAsync(vase, quantite: 1m);
        await _atelier.VendreAsync(tasse, quantite: 4m, prix: 800m);

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.ProduitsLesPlusVendus));

        rapport.Lignes.Select(l => l[0]).Should().Equal("Tasse", "Vase");
    }

    [Fact]
    public async Task L_etat_du_stock_signale_les_ruptures_et_les_pieces_uniques()
    {
        await _atelier.CreerProduitAsync("Vase", stockInitial: 10m, stockMinimum: 2m);
        await _atelier.CreerProduitAsync("Bol", stockInitial: 1m, stockMinimum: 2m);
        await _atelier.CreerProduitAsync("Tasse", stockInitial: 0m);
        var piece = await _atelier.CreerProduitAsync("Jarre signée", stockInitial: 1m, pieceUnique: true);
        await _atelier.VendreAsync(piece);

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.ValeurStock));

        string Etat(string produit) => rapport.Lignes.Single(l => l[1] == produit)[4];
        Etat("Vase").Should().Be("Disponible");
        Etat("Bol").Should().Be("Stock faible");
        Etat("Tasse").Should().Be("Rupture");
        Etat("Jarre signée").Should().Be("Vendue");
        rapport.Totaux![4].Should().Be("2 à surveiller");
    }

    [Fact]
    public async Task Un_rapport_sans_donnees_ne_renvoie_aucune_ligne()
    {
        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.VentesParJour));

        rapport.Lignes.Should().BeEmpty();
        rapport.Colonnes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task L_export_csv_commence_par_le_bom_et_reprend_les_colonnes()
    {
        await _atelier.CreerDepenseAsync(3000m);

        var (nom, contenu) = await _atelier.Rapports.ExporterCsvAsync(Mois(TypeRapport.Depenses));

        nom.Should().EndWith(".csv");
        contenu.Take(3).Should().Equal(Encoding.UTF8.GetPreamble());

        var texte = Encoding.UTF8.GetString(contenu);
        texte.Should().Contain("Dépenses");
        texte.Should().Contain("Catégorie;Nombre;Montant");
        texte.Should().Contain("Électricité");
    }

    [Fact]
    public async Task L_export_csv_protege_les_valeurs_contenant_un_point_virgule()
    {
        var categorie = await _atelier.Referentiels.CreerAsync(
            TypeReferentiel.CategorieDepense, new ElementReferentielRequete { Nom = "Gaz ; butane" });
        await _atelier.Depenses.CreerAsync(new DepenseRequete
        {
            CategorieId = categorie.Id, Montant = 900m, Description = "Bouteille"
        });

        var (_, contenu) = await _atelier.Rapports.ExporterCsvAsync(Mois(TypeRapport.Depenses));

        Encoding.UTF8.GetString(contenu).Should().Contain("\"Gaz ; butane\"");
    }

    public void Dispose() => _atelier.Dispose();
}
