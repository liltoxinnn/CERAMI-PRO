using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Codes;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Services;
using CeramiPro.Domain.Common;
using CeramiPro.Tests.Aides;
using FluentAssertions;

namespace CeramiPro.Tests.Codes;

public class CodeServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    public CodeServiceTests() => _atelier.AccorderTousLesDroits();

    [Fact]
    public async Task L_etiquette_reprend_le_nom_le_prix_et_les_deux_codes()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase décoratif", prixVente: 3500m);

        var etiquette = await _atelier.Codes.EtiquetteProduitAsync(produitId);

        etiquette.Nom.Should().Be("Vase décoratif");
        etiquette.Categorie.Should().Be("Vases décoratifs");
        etiquette.PrixVente.Should().Be(3500m);
        etiquette.PrixAffiche.Should().Contain("DA");
        etiquette.CodeBarres.Should().StartWith("PRD-");
        etiquette.CodeQr.Should().StartWith("PRD-");
        etiquette.CodeQrSvg.Should().StartWith("<svg");
        etiquette.CodeBarresSvg.Should().StartWith("<svg");
    }

    [Fact]
    public async Task Une_etiquette_demandee_pour_un_produit_inconnu_est_refusee()
    {
        var action = async () => await _atelier.Codes.EtiquetteProduitAsync(9999);

        await action.Should().ThrowAsync<IntrouvableException>();
    }

    [Fact]
    public async Task Une_planche_repete_chaque_produit_autant_de_fois_que_demande()
    {
        var vase = await _atelier.CreerProduitAsync("Vase");
        var assiette = await _atelier.CreerProduitAsync("Assiette");

        var etiquettes = await _atelier.Codes.EtiquettesAsync(new EtiquettesRequete
        {
            ProduitIds = new List<int> { vase, assiette },
            Exemplaires = 3
        });

        etiquettes.Should().HaveCount(6);
        etiquettes.Count(e => e.Nom == "Vase").Should().Be(3);
        etiquettes.Count(e => e.Nom == "Assiette").Should().Be(3);
    }

    [Fact]
    public async Task Une_planche_sans_produit_est_refusee()
    {
        var action = async () => await _atelier.Codes.EtiquettesAsync(new EtiquettesRequete());

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*au moins un produit*");
    }

    [Fact]
    public async Task Une_planche_trop_grande_est_refusee()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase");

        var action = async () => await _atelier.Codes.EtiquettesAsync(new EtiquettesRequete
        {
            ProduitIds = new List<int> { produitId },
            Exemplaires = CodeService.EtiquettesMaximum + 1
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*étiquettes*");
    }

    [Fact]
    public async Task Scanner_la_reference_d_un_produit_ouvre_sa_fiche()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m, stockInitial: 4m);
        var etiquette = await _atelier.Codes.EtiquetteProduitAsync(produitId);

        var resultat = await _atelier.Codes.ResoudreAsync(etiquette.CodeBarres);

        resultat.Trouve.Should().BeTrue();
        resultat.Cible.Should().Be(CibleScan.Produit);
        resultat.Id.Should().Be(produitId);
        resultat.Libelle.Should().Be("Vase");
        resultat.Details.Should().Contain("4").And.Contain("stock");
        resultat.Adresse.Should().StartWith("produits?recherche=");
    }

    [Fact]
    public async Task Le_scan_ne_tient_pas_compte_de_la_casse()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase");
        var etiquette = await _atelier.Codes.EtiquetteProduitAsync(produitId);

        var resultat = await _atelier.Codes.ResoudreAsync(etiquette.CodeQr.ToLowerInvariant());

        resultat.Trouve.Should().BeTrue();
        resultat.Id.Should().Be(produitId);
    }

    [Fact]
    public async Task Scanner_une_matiere_ouvre_l_ecran_du_stock()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile", stockInitial: 50m);
        var matiere = await _atelier.Matieres.ObtenirAsync(matiereId);

        var resultat = await _atelier.Codes.ResoudreAsync(matiere.Reference);

        resultat.Trouve.Should().BeTrue();
        resultat.Cible.Should().Be(CibleScan.Matiere);
        resultat.Adresse.Should().StartWith("matieres?recherche=");
    }

    [Fact]
    public async Task Scanner_un_ordre_de_production_ouvre_l_atelier()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();

        var ordre = await _atelier.Production.CreerAsync(new Application.DTOs.Production.OrdreProductionRequete
        {
            ProduitId = produitId,
            QuantitePrevue = 5m
        });

        var resultat = await _atelier.Codes.ResoudreAsync(ordre.Numero);

        resultat.Trouve.Should().BeTrue();
        resultat.Cible.Should().Be(CibleScan.OrdreProduction);
        resultat.Id.Should().Be(ordre.Id);
        resultat.Adresse.Should().StartWith("production?recherche=");
    }

    [Fact]
    public async Task Scanner_une_vente_ouvre_son_historique()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m, stockInitial: 10m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        var resultat = await _atelier.Codes.ResoudreAsync(vente.Numero);

        resultat.Trouve.Should().BeTrue();
        resultat.Cible.Should().Be(CibleScan.Vente);
        resultat.Adresse.Should().StartWith("ventes?recherche=");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("CODE-INCONNU-9999")]
    public async Task Un_code_inconnu_est_signale_sans_erreur(string code)
    {
        var resultat = await _atelier.Codes.ResoudreAsync(code);

        resultat.Trouve.Should().BeFalse();
        resultat.Cible.Should().Be(CibleScan.Inconnu);
        resultat.Adresse.Should().BeNull();
        resultat.Libelle.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Un_utilisateur_sans_droit_sur_les_produits_ne_les_trouve_pas()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase");
        var etiquette = await _atelier.Codes.EtiquetteProduitAsync(produitId);

        _atelier.UtilisateurCourant.Droits.Remove(PermissionCodes.ProduitsConsulter);

        var resultat = await _atelier.Codes.ResoudreAsync(etiquette.CodeBarres);

        resultat.Trouve.Should().BeFalse();
        resultat.Cible.Should().Be(CibleScan.Inconnu);
    }

    public void Dispose() => _atelier.Dispose();
}
