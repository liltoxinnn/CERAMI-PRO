using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Catalogue;
using CeramicWorkshop.Domain.Enums;
using CeramicWorkshop.UnitTests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.UnitTests.Catalogue;

public class ProduitServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    [Fact]
    public async Task Un_produit_recoit_une_reference_et_un_code_barres()
    {
        var id = await _atelier.CreerProduitAsync("Vase décoratif A");

        var produit = await _atelier.Produits.ObtenirAsync(id);

        produit.Reference.Should().StartWith("PRD-");
        produit.CodeBarres.Should().Be(produit.Reference);
        produit.QrCode.Should().Be(produit.Reference);
    }

    [Fact]
    public async Task Le_benefice_par_piece_est_calcule()
    {
        var id = await _atelier.CreerProduitAsync("Statue", prixVente: 3500m, coutProduction: 1850m);

        var produit = await _atelier.Produits.ObtenirAsync(id);

        produit.Marge.Should().Be(1650m);
        produit.TauxMarge.Should().BeApproximately(47.1m, 0.1m);
    }

    [Fact]
    public async Task Le_stock_initial_est_enregistre_comme_mouvement()
    {
        var id = await _atelier.CreerProduitAsync("Assiette", stockInitial: 12m);

        (await _atelier.StockProduitAsync(id)).Should().Be(12m);

        var mouvement = await _atelier.Contexte.InventoryTransactions.AsNoTracking()
            .SingleAsync(t => t.ProductId == id);
        mouvement.ItemType.Should().Be(InventoryItemType.ProduitFini);
        mouvement.QuantityAfter.Should().Be(12m);
    }

    [Fact]
    public async Task Un_code_barres_ne_peut_pas_etre_utilise_deux_fois()
    {
        await _atelier.Produits.CreerAsync(new ProduitRequete
        {
            Nom = "Pot", CategorieId = _atelier.CategorieProduitId, CodeBarres = "6130000000012"
        });

        var action = async () => await _atelier.Produits.CreerAsync(new ProduitRequete
        {
            Nom = "Pot bis", CategorieId = _atelier.CategorieProduitId, CodeBarres = "6130000000012"
        });

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*déjà utilisé*");
    }

    [Fact]
    public async Task Un_produit_est_retrouve_par_son_code_barres()
    {
        var id = await _atelier.CreerProduitAsync("Décoration murale");
        var produit = await _atelier.Produits.ObtenirAsync(id);

        var trouve = await _atelier.Produits.RechercherParCodeAsync(produit.CodeBarres!);

        trouve.Should().NotBeNull();
        trouve!.Id.Should().Be(id);
    }

    [Fact]
    public async Task Un_code_inconnu_ne_renvoie_aucun_produit()
        => (await _atelier.Produits.RechercherParCodeAsync("inexistant")).Should().BeNull();

    [Fact]
    public async Task Une_variante_herite_du_prix_du_produit_avec_son_ecart()
    {
        var id = await _atelier.CreerProduitAsync("Vase", prixVente: 3000m);

        var variante = await _atelier.Produits.AjouterVarianteAsync(id, new VarianteProduitRequete
        {
            Nom = "Grand modèle", AjustementPrix = 800m
        });

        variante.PrixFinal.Should().Be(3800m);
        variante.Reference.Should().EndWith("-01");
    }

    [Fact]
    public async Task La_premiere_photo_devient_la_photo_principale()
    {
        var id = await _atelier.CreerProduitAsync("Sculpture");

        var photo = await _atelier.Produits.AjouterPhotoAsync(id, new PhotoProduitRequete
        {
            Chemin = "/fichiers/2026-03/photo.jpg", Principale = true
        });

        photo.Principale.Should().BeTrue();
        (await _atelier.Produits.ObtenirAsync(id)).ImagePrincipale.Should().Be("/fichiers/2026-03/photo.jpg");
    }

    [Fact]
    public async Task Un_produit_avec_historique_ne_peut_pas_etre_supprime()
    {
        var id = await _atelier.CreerProduitAsync("Vase", stockInitial: 5m);

        var action = async () => await _atelier.Produits.SupprimerAsync(id);

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Désactivez-le*");
    }

    public void Dispose() => _atelier.Dispose();
}

public class RecetteServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    /// <summary>Recette du prompt : vase décoratif A pour une pièce.</summary>
    private async Task<(int RecetteId, int ArgileId, int EmailId)> CreerRecetteVaseAsync(
        decimal stockArgile = 100m, decimal stockEmail = 5m)
    {
        var produitId = await _atelier.CreerProduitAsync("Vase décoratif A");
        var argileId = await _atelier.CreerMatiereAsync("Argile", stockInitial: stockArgile, prix: 200m);
        var emailId = await _atelier.CreerMatiereAsync("Émail", stockInitial: stockEmail, prix: 2500m);

        var recette = await _atelier.Recettes.CreerAsync(new RecetteRequete
        {
            ProduitId = produitId,
            Nom = "Vase décoratif A",
            Rendement = 1m,
            CoutMainOeuvre = 600m,
            CoutCuisson = 300m,
            CoutEmballage = 50m,
            Lignes = new List<LigneRecetteRequete>
            {
                new() { MatiereId = argileId, UniteId = _atelier.UniteKiloId, Quantite = 1.5m },
                new() { MatiereId = emailId, UniteId = _atelier.UniteKiloId, Quantite = 0.1m }
            }
        });

        return (recette.Id, argileId, emailId);
    }

    [Fact]
    public async Task La_premiere_recette_d_un_produit_devient_la_reference()
    {
        var (recetteId, _, _) = await CreerRecetteVaseAsync();

        (await _atelier.Recettes.ObtenirAsync(recetteId)).ParDefaut.Should().BeTrue();
    }

    [Fact]
    public async Task Le_cout_de_la_recette_additionne_matieres_et_frais()
    {
        var (recetteId, _, _) = await CreerRecetteVaseAsync();

        var recette = await _atelier.Recettes.ObtenirAsync(recetteId);

        // 1,5 kg à 200 DA + 0,1 kg à 2 500 DA = 550 DA de matières.
        recette.CoutMatieres.Should().Be(550m);
        recette.CoutTotal.Should().Be(1500m);
        recette.CoutUnitaire.Should().Be(1500m);
    }

    [Fact]
    public async Task Le_calcul_des_besoins_multiplie_par_la_quantite_a_produire()
    {
        var (recetteId, _, _) = await CreerRecetteVaseAsync();

        var besoins = await _atelier.Recettes.CalculerBesoinsAsync(recetteId, 20m);

        besoins.Besoins.Single(b => b.MatiereNom == "Argile").QuantiteNecessaire.Should().Be(30m);
        besoins.Besoins.Single(b => b.MatiereNom == "Émail").QuantiteNecessaire.Should().Be(2m);
        besoins.QuantiteAProduire.Should().Be(20m);
    }

    [Fact]
    public async Task Le_calcul_signale_les_matieres_manquantes()
    {
        var (recetteId, _, _) = await CreerRecetteVaseAsync(stockArgile: 22m);

        var besoins = await _atelier.Recettes.CalculerBesoinsAsync(recetteId, 20m);

        besoins.MatieresSuffisantes.Should().BeFalse();

        var argile = besoins.Besoins.Single(b => b.MatiereNom == "Argile");
        argile.QuantiteNecessaire.Should().Be(30m);
        argile.QuantiteDisponible.Should().Be(22m);
        argile.Manquant.Should().Be(8m);
    }

    [Fact]
    public async Task Le_pourcentage_de_perte_augmente_la_quantite_necessaire()
    {
        var produitId = await _atelier.CreerProduitAsync("Assiette");
        var argileId = await _atelier.CreerMatiereAsync("Argile", stockInitial: 100m, prix: 100m);

        var recette = await _atelier.Recettes.CreerAsync(new RecetteRequete
        {
            ProduitId = produitId,
            Nom = "Assiette standard",
            Rendement = 1m,
            Lignes = new List<LigneRecetteRequete>
            {
                new() { MatiereId = argileId, UniteId = _atelier.UniteKiloId, Quantite = 1m, PourcentagePerte = 10m }
            }
        });

        var besoins = await _atelier.Recettes.CalculerBesoinsAsync(recette.Id, 10m);

        besoins.Besoins.Single().QuantiteNecessaire.Should().Be(11m);
    }

    [Fact]
    public async Task Le_rendement_est_pris_en_compte()
    {
        var produitId = await _atelier.CreerProduitAsync("Petit pot");
        var argileId = await _atelier.CreerMatiereAsync("Argile", stockInitial: 100m, prix: 100m);

        // 5 kg d'argile donnent 10 pots : 20 pots demandent donc 10 kg.
        var recette = await _atelier.Recettes.CreerAsync(new RecetteRequete
        {
            ProduitId = produitId,
            Nom = "Série de 10 pots",
            Rendement = 10m,
            Lignes = new List<LigneRecetteRequete>
            {
                new() { MatiereId = argileId, UniteId = _atelier.UniteKiloId, Quantite = 5m }
            }
        });

        var besoins = await _atelier.Recettes.CalculerBesoinsAsync(recette.Id, 20m);

        besoins.Besoins.Single().QuantiteNecessaire.Should().Be(10m);
    }

    [Fact]
    public async Task Une_matiere_ne_peut_pas_figurer_deux_fois_dans_une_recette()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase");
        var argileId = await _atelier.CreerMatiereAsync("Argile", stockInitial: 10m);

        var action = async () => await _atelier.Recettes.CreerAsync(new RecetteRequete
        {
            ProduitId = produitId,
            Nom = "Doublon",
            Rendement = 1m,
            Lignes = new List<LigneRecetteRequete>
            {
                new() { MatiereId = argileId, UniteId = _atelier.UniteKiloId, Quantite = 1m },
                new() { MatiereId = argileId, UniteId = _atelier.UniteKiloId, Quantite = 2m }
            }
        });

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*une seule fois*");
    }

    [Fact]
    public async Task Une_recette_sans_matiere_est_refusee()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase");

        var action = async () => await _atelier.Recettes.CreerAsync(new RecetteRequete
        {
            ProduitId = produitId, Nom = "Vide", Rendement = 1m, Lignes = new List<LigneRecetteRequete>()
        });

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*au moins une matière*");
    }

    [Fact]
    public async Task Une_seule_recette_de_reference_par_produit()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase");
        var argileId = await _atelier.CreerMatiereAsync("Argile", stockInitial: 50m);

        var premiere = await _atelier.Recettes.CreerAsync(new RecetteRequete
        {
            ProduitId = produitId, Nom = "Version 1", Rendement = 1m,
            Lignes = new List<LigneRecetteRequete>
                { new() { MatiereId = argileId, UniteId = _atelier.UniteKiloId, Quantite = 1m } }
        });

        var seconde = await _atelier.Recettes.CreerAsync(new RecetteRequete
        {
            ProduitId = produitId, Nom = "Version 2", Rendement = 1m, ParDefaut = true,
            Lignes = new List<LigneRecetteRequete>
                { new() { MatiereId = argileId, UniteId = _atelier.UniteKiloId, Quantite = 2m } }
        });

        (await _atelier.Recettes.ObtenirAsync(premiere.Id)).ParDefaut.Should().BeFalse();
        (await _atelier.Recettes.ObtenirAsync(seconde.Id)).ParDefaut.Should().BeTrue();
        seconde.Version.Should().Be(2);
    }

    public void Dispose() => _atelier.Dispose();
}
