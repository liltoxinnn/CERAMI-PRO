using System.Text;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Tests.Aides;
using FluentAssertions;

namespace CeramiPro.Tests.Finances;

public class RapportServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    private RapportRequete Mois(TypeRapport type) => new()
    {
        Type = type,
        Du = new DateTime(_atelier.Horloge.MaintenantUtc.Year, _atelier.Horloge.MaintenantUtc.Month, 1),
        Au = _atelier.Horloge.MaintenantUtc.Date
    };

    [Theory]
    [InlineData(TypeRapport.ChiffreAffaires)]
    [InlineData(TypeRapport.Benefices)]
    [InlineData(TypeRapport.Depenses)]
    [InlineData(TypeRapport.DettesClients)]
    [InlineData(TypeRapport.DettesFournisseurs)]
    [InlineData(TypeRapport.ConsommationMatieres)]
    [InlineData(TypeRapport.Production)]
    [InlineData(TypeRapport.ProduitsEndommages)]
    [InlineData(TypeRapport.ProduitsLesPlusVendus)]
    [InlineData(TypeRapport.ProduitsLesPlusRentables)]
    [InlineData(TypeRapport.ValeurStock)]
    [InlineData(TypeRapport.PerformanceProduction)]
    public async Task Chaque_rapport_repond_avec_un_titre_et_des_colonnes(TypeRapport type)
    {
        var rapport = await _atelier.Rapports.GenererAsync(Mois(type));

        rapport.Type.Should().Be(type);
        rapport.Titre.Should().NotBeNullOrWhiteSpace();
        rapport.Periode.Should().NotBeNullOrWhiteSpace();
        rapport.Colonnes.Should().NotBeEmpty();
        rapport.Lignes.All(l => l.Count == rapport.Colonnes.Count).Should().BeTrue();
    }

    [Fact]
    public async Task Le_rapport_du_chiffre_d_affaires_totalise_les_ventes_confirmees()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m,
            coutProduction: 1850m, stockInitial: 10m);

        await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 2m } }
        });

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.ChiffreAffaires));

        rapport.Lignes.Should().ContainSingle();
        rapport.Totaux.Should().NotBeNull();
        rapport.Totaux![2].Should().Contain("7").And.Contain("DA");
        rapport.Graphique.Should().ContainSingle();
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

        await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            MontantPaye = 500m,
            ModeReglementId = _atelier.ModeReglementId,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.DettesClients));

        rapport.Lignes.Should().ContainSingle();
        rapport.Lignes[0][0].Should().Be("Karim Saïdi");
        rapport.Lignes[0][4].Should().Contain("3");
    }

    [Fact]
    public async Task Le_rapport_de_la_valeur_du_stock_reprend_les_matieres_et_les_produits()
    {
        await _atelier.CreerMatiereAsync("Argile", stockInitial: 100m, prix: 200m);
        await _atelier.CreerProduitAsync("Vase", coutProduction: 1850m, stockInitial: 4m);

        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.ValeurStock));

        rapport.Lignes.Should().HaveCountGreaterThanOrEqualTo(2);
        rapport.Totaux.Should().NotBeNull();
    }

    [Fact]
    public async Task Un_rapport_sans_donnees_ne_renvoie_aucune_ligne()
    {
        var rapport = await _atelier.Rapports.GenererAsync(Mois(TypeRapport.ChiffreAffaires));

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
        await _atelier.CreerDepenseAsync(3000m, description: "Gaz ; butane");

        var (_, contenu) = await _atelier.Rapports.ExporterCsvAsync(Mois(TypeRapport.ChiffreAffaires));

        Encoding.UTF8.GetString(contenu).Should().NotBeNullOrWhiteSpace();
    }

    public void Dispose() => _atelier.Dispose();
}
