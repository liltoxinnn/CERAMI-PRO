using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Tests.Aides;
using FluentAssertions;

namespace CeramiPro.Tests.Catalogue;

public class ProduitServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    public void Dispose() => _atelier.Dispose();

    [Fact]
    public async Task Un_produit_recoit_une_reference_de_l_atelier()
    {
        var premier = await _atelier.Produits.ObtenirAsync(await _atelier.CreerProduitAsync("Vase décoratif A"));
        var second = await _atelier.Produits.ObtenirAsync(await _atelier.CreerProduitAsync("Vase décoratif B"));

        premier.Reference.Should().StartWith("PRD-");
        second.Reference.Should().NotBe(premier.Reference);
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
    public async Task Un_produit_sans_nom_ou_sans_categorie_est_refuse()
    {
        var sansNom = () => _atelier.Produits.CreerAsync(new ProduitRequete
        {
            Nom = " ", CategorieId = _atelier.CategorieProduitId, PrixVente = 100m
        });
        var sansCategorie = () => _atelier.Produits.CreerAsync(new ProduitRequete { Nom = "Vase", PrixVente = 100m });

        await sansNom.Should().ThrowAsync<Exception>();
        await sansCategorie.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Un_prix_negatif_est_refuse()
    {
        var action = () => _atelier.CreerProduitAsync("Vase", prixVente: -10m);

        await action.Should().ThrowAsync<RegleMetierException>();
    }

    [Fact]
    public async Task Modifier_un_produit_ne_touche_pas_a_son_stock()
    {
        var id = await _atelier.CreerProduitAsync("Vase", prixVente: 3000m, stockInitial: 7m);

        await _atelier.Produits.ModifierAsync(id, new ProduitRequete
        {
            Nom = "Vase bleu", CategorieId = _atelier.CategorieProduitId, PrixVente = 3200m,
            CoutProduction = 1500m, StockMinimum = 2m, Description = "Émail bleu de cobalt"
        });

        var produit = await _atelier.Produits.ObtenirAsync(id);
        produit.Nom.Should().Be("Vase bleu");
        produit.PrixVente.Should().Be(3200m);
        produit.Description.Should().Be("Émail bleu de cobalt");

        // Le stock ne bouge qu'avec un mouvement tracé, jamais en modifiant la fiche.
        produit.StockActuel.Should().Be(7m);
    }

    [Fact]
    public async Task Une_piece_unique_n_a_pas_de_seuil_d_alerte()
    {
        var id = await _atelier.CreerProduitAsync("Jarre signée", stockInitial: 1m, stockMinimum: 3m, pieceUnique: true);

        var produit = await _atelier.Produits.ObtenirAsync(id);

        produit.PieceUnique.Should().BeTrue();
        produit.StockMinimum.Should().Be(0m);
        produit.StockFaible.Should().BeFalse();
    }

    [Fact]
    public async Task Une_piece_vendue_n_est_pas_signalee_comme_a_refabriquer()
    {
        var piece = await _atelier.CreerProduitAsync("Jarre signée", stockInitial: 1m, pieceUnique: true);
        await _atelier.VendreAsync(piece, prix: 25000m);
        await _atelier.CreerProduitAsync("Bol", stockInitial: 1m, stockMinimum: 3m);

        var aRefabriquer = await _atelier.Produits.ListerStockFaibleAsync();

        aRefabriquer.Select(p => p.Nom).Should().Equal("Bol");
    }

    [Fact]
    public async Task Un_produit_en_plusieurs_exemplaires_ne_peut_pas_devenir_piece_unique()
    {
        var id = await _atelier.CreerProduitAsync("Tasse", stockInitial: 6m);

        var action = () => _atelier.Produits.ModifierAsync(id, new ProduitRequete
        {
            Nom = "Tasse", CategorieId = _atelier.CategorieProduitId, PrixVente = 800m, PieceUnique = true
        });

        await action.Should().ThrowAsync<RegleMetierException>();
    }

    [Fact]
    public async Task Le_filtre_du_stock_faible_ne_garde_que_ce_qui_est_a_refabriquer()
    {
        await _atelier.CreerProduitAsync("Vase", stockInitial: 10m, stockMinimum: 2m);
        await _atelier.CreerProduitAsync("Bol", stockInitial: 1m, stockMinimum: 2m);

        var liste = await _atelier.Produits.ListerAsync(new FiltreProduitsRequete { SeulementStockFaible = true });

        liste.Elements.Should().ContainSingle().Which.Nom.Should().Be("Bol");
    }

    [Fact]
    public async Task La_recherche_trouve_un_produit_par_son_nom_ou_sa_reference()
    {
        var id = await _atelier.CreerProduitAsync("Plat à tajine");
        await _atelier.CreerProduitAsync("Vase");
        var reference = (await _atelier.Produits.ObtenirAsync(id)).Reference;

        (await _atelier.Produits.ListerAsync(new FiltreProduitsRequete { Recherche = "tajine" }))
            .Elements.Should().ContainSingle().Which.Id.Should().Be(id);
        (await _atelier.Produits.ListerAsync(new FiltreProduitsRequete { Recherche = reference }))
            .Elements.Should().ContainSingle().Which.Id.Should().Be(id);
    }

    [Fact]
    public async Task La_synthese_du_catalogue_valorise_le_stock_au_cout()
    {
        await _atelier.CreerProduitAsync("Vase", coutProduction: 1000m, stockInitial: 3m, stockMinimum: 1m);
        await _atelier.CreerProduitAsync("Bol", coutProduction: 200m, stockInitial: 1m, stockMinimum: 2m);

        var synthese = await _atelier.Produits.SyntheseAsync();

        synthese.NombreProduits.Should().Be(2);
        synthese.NombreStockFaible.Should().Be(1);
        synthese.ValeurStock.Should().Be(3200m);
    }

    // ------------------------------------------------------------- Photos

    [Fact]
    public async Task La_premiere_photo_devient_la_photo_principale()
    {
        var id = await _atelier.CreerProduitAsync("Sculpture");

        var photo = await _atelier.Produits.AjouterPhotoAsync(id, new PhotoProduitRequete
        {
            Chemin = "2026-08/sculpture.jpg"
        });

        photo.Principale.Should().BeTrue();
        (await _atelier.Produits.ObtenirAsync(id)).ImagePrincipale.Should().Be("2026-08/sculpture.jpg");
    }

    [Fact]
    public async Task Choisir_une_autre_photo_principale_remplace_la_precedente()
    {
        var id = await _atelier.CreerProduitAsync("Sculpture");
        await _atelier.Produits.AjouterPhotoAsync(id, new PhotoProduitRequete { Chemin = "a.jpg" });
        var seconde = await _atelier.Produits.AjouterPhotoAsync(id, new PhotoProduitRequete { Chemin = "b.jpg" });

        await _atelier.Produits.DefinirPhotoPrincipaleAsync(id, seconde.Id);

        var photos = await _atelier.Produits.ListerPhotosAsync(id);
        photos.Should().ContainSingle(p => p.Principale).Which.Chemin.Should().Be("b.jpg");
        (await _atelier.Produits.ObtenirAsync(id)).NombrePhotos.Should().Be(2);
    }

    [Fact]
    public async Task Retirer_la_photo_principale_en_promeut_une_autre()
    {
        var id = await _atelier.CreerProduitAsync("Sculpture");
        var premiere = await _atelier.Produits.AjouterPhotoAsync(id, new PhotoProduitRequete { Chemin = "a.jpg" });
        await _atelier.Produits.AjouterPhotoAsync(id, new PhotoProduitRequete { Chemin = "b.jpg" });

        var chemin = await _atelier.Produits.SupprimerPhotoAsync(id, premiere.Id);

        // Le chemin renvoyé permet d'effacer le fichier du disque.
        chemin.Should().Be("a.jpg");
        (await _atelier.Produits.ListerPhotosAsync(id)).Should().ContainSingle(p => p.Principale && p.Chemin == "b.jpg");
    }

    // ------------------------------------------------------------- Suppression

    [Fact]
    public async Task Un_produit_avec_historique_ne_peut_pas_etre_supprime()
    {
        var id = await _atelier.CreerProduitAsync("Vase", stockInitial: 5m);

        var action = () => _atelier.Produits.SupprimerAsync(id);

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*Désactivez-le*");
    }

    [Fact]
    public async Task Un_produit_saisi_par_erreur_et_jamais_utilise_se_supprime()
    {
        var id = await _atelier.CreerProduitAsync("Vase");

        await _atelier.Produits.SupprimerAsync(id);

        (await _atelier.Produits.ListerAsync(new FiltreProduitsRequete())).Elements.Should().BeEmpty();
        _atelier.Audit.Traces.Should().Contain(t => t.Description!.Contains("Suppression du produit"));
    }

    [Fact]
    public async Task Un_produit_desactive_n_est_plus_propose_a_la_vente()
    {
        var id = await _atelier.CreerProduitAsync("Vase", stockInitial: 5m);

        await _atelier.Produits.ModifierAsync(id, new ProduitRequete
        {
            Nom = "Vase", CategorieId = _atelier.CategorieProduitId, PrixVente = 3500m, Actif = false
        });

        (await _atelier.Produits.ListerActifsAsync()).Should().BeEmpty();
    }
}
