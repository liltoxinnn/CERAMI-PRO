using CeramicWorkshop.Application.DTOs.Commercial;
using CeramicWorkshop.Application.DTOs.Recherche;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.UnitTests.Aides;
using FluentAssertions;

namespace CeramicWorkshop.UnitTests.General;

public class RechercheServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    public RechercheServiceTests() => _atelier.AccorderTousLesDroits();

    [Fact]
    public async Task Un_produit_est_retrouve_par_son_nom()
    {
        await _atelier.CreerProduitAsync("Vase décoratif bleu", prixVente: 3500m);

        var resultat = await _atelier.Recherche.ChercherAsync("vase");

        resultat.Total.Should().Be(1);
        resultat.Groupes.Should().ContainSingle()
            .Which.Famille.Should().Be(FamilleResultat.Produit);
        resultat.Groupes[0].Resultats[0].Titre.Should().Be("Vase décoratif bleu");
        resultat.Groupes[0].Resultats[0].Adresse.Should().StartWith("produits?recherche=");
    }

    [Fact]
    public async Task Un_produit_est_retrouve_sans_les_accents()
    {
        await _atelier.CreerProduitAsync("Assiette émaillée");

        var resultat = await _atelier.Recherche.ChercherAsync("emaillee");

        resultat.Total.Should().BeGreaterThan(0);
        resultat.Groupes.SelectMany(g => g.Resultats)
            .Should().Contain(r => r.Titre == "Assiette émaillée");
    }

    [Fact]
    public async Task Un_nom_mal_orthographie_est_retrouve()
    {
        await _atelier.CreerClientAsync("Mohamed Benali");

        var resultat = await _atelier.Recherche.ChercherAsync("benalli");

        resultat.Groupes.SelectMany(g => g.Resultats)
            .Should().Contain(r => r.Titre == "Mohamed Benali");
    }

    [Fact]
    public async Task Une_reference_retrouve_sa_fiche()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase");
        var etiquette = await _atelier.Codes.EtiquetteProduitAsync(produitId);

        var resultat = await _atelier.Recherche.ChercherAsync(etiquette.CodeBarres);

        resultat.Groupes.SelectMany(g => g.Resultats).Should().Contain(r => r.Id == produitId);
    }

    [Fact]
    public async Task La_recherche_couvre_plusieurs_familles()
    {
        await _atelier.CreerProduitAsync("Poterie de Tlemcen");
        await _atelier.CreerClientAsync("Poterie du Sud");

        var resultat = await _atelier.Recherche.ChercherAsync("poterie");

        resultat.Groupes.Select(g => g.Famille)
            .Should().Contain(new[] { FamilleResultat.Produit, FamilleResultat.Client });
    }

    [Fact]
    public async Task Une_famille_interdite_n_apparait_pas_dans_les_resultats()
    {
        await _atelier.CreerProduitAsync("Vase décoratif");

        _atelier.UtilisateurCourant.Droits.Remove(PermissionCodes.ProduitsConsulter);

        var resultat = await _atelier.Recherche.ChercherAsync("vase");

        resultat.Groupes.Should().NotContain(g => g.Famille == FamilleResultat.Produit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public async Task Un_terme_trop_court_ne_declenche_pas_de_recherche(string terme)
    {
        await _atelier.CreerProduitAsync("Vase");

        var resultat = await _atelier.Recherche.ChercherAsync(terme);

        resultat.Total.Should().Be(0);
        resultat.Groupes.Should().BeEmpty();
    }

    [Fact]
    public async Task Une_vente_est_retrouvee_par_son_numero()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m, stockInitial: 5m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        var resultat = await _atelier.Recherche.ChercherAsync(vente.Numero);

        resultat.Groupes.Should().Contain(g => g.Famille == FamilleResultat.Vente);
    }

    [Fact]
    public async Task Aucun_resultat_quand_rien_ne_correspond()
    {
        await _atelier.CreerProduitAsync("Vase");

        var resultat = await _atelier.Recherche.ChercherAsync("motoculteur");

        resultat.Total.Should().Be(0);
    }

    public void Dispose() => _atelier.Dispose();
}
