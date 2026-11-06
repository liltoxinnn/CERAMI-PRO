using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Presentation.ViewModels.Ecrans;
using CeramiPro.Tests.Aides;
using FluentAssertions;

namespace CeramiPro.Tests;

/// <summary>
/// La caisse, de bout en bout : on choisit un produit, on l'ajoute, on
/// encaisse, et le stock diminue.
///
/// Les documents PDF ne sont pas produits ici — ils ont leurs propres
/// vérifications — mais tout le reste passe par les vrais services métier.
/// </summary>
public class CaisseTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();
    private readonly DialogueFactice _dialogue = new();
    private readonly FichierFactice _fichiers = new();

    public void Dispose() => _atelier.Dispose();

    private CaisseVueModele Caisse() => new(
        _atelier.Ventes, _atelier.Produits, _atelier.Clients, ReferentielFactice(),
        DocumentFactice(), _fichiers, _dialogue);

    /// <summary>
    /// Les modes de règlement et les documents ne sont pas au cœur de ces
    /// vérifications : des doubles suffisent, et évitent de dépendre de
    /// l'amorçage complet de la base.
    /// </summary>
    private CeramiPro.Application.Interfaces.IReferentielService ReferentielFactice()
        => new ReferentielMinimal(_atelier.ModeReglementId);

    private CeramiPro.Application.Interfaces.IDocumentService DocumentFactice()
        => new DocumentMinimal();

    [Fact]
    public async Task Le_catalogue_est_propose_a_l_ouverture()
    {
        await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.Articles.Should().ContainSingle(a => a.Libelle.Contains("Vase bleu"));
    }

    [Fact]
    public async Task Choisir_un_produit_propose_son_prix_de_vente()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = produitId;

        // Le prix du catalogue évite de le retaper à chaque vente.
        caisse.PrixUnitaire.Should().Be(4200m);
    }

    [Fact]
    public async Task Ajouter_deux_fois_le_meme_produit_augmente_la_ligne()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = produitId;
        caisse.AjouterLigneCommand.Execute(null);

        caisse.ArticleChoisi = produitId;
        caisse.AjouterLigneCommand.Execute(null);

        // Choisir deux fois le même produit donne « 2 », pas deux lignes.
        caisse.Lignes.Should().ContainSingle();
        caisse.Lignes[0].Quantite.Should().Be(2m);
        caisse.Total.Should().Be(8400m);
    }

    [Fact]
    public async Task Le_rendu_de_monnaie_suit_le_montant_recu()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = produitId;
        caisse.AjouterLigneCommand.Execute(null);

        caisse.MontantPaye = 5000m;

        caisse.Rendu.Should().Be(800m);
        caisse.Reste.Should().Be(0m);
    }

    [Fact]
    public async Task Un_reglement_partiel_laisse_un_reste_du()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = produitId;
        caisse.AjouterLigneCommand.Execute(null);

        caisse.MontantPaye = 2000m;

        caisse.Rendu.Should().Be(0m);
        caisse.Reste.Should().Be(2200m);
    }

    [Fact]
    public async Task Une_vente_reglee_en_partie_exige_un_client()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = produitId;
        caisse.AjouterLigneCommand.Execute(null);
        caisse.MontantPaye = 2000m;

        await caisse.EnregistrerCommand.ExecuteAsync(null);

        // Sans client, le reste dû ne pourrait être réclamé à personne.
        caisse.MessageErreur.Should().Contain("client");
        caisse.Lignes.Should().ContainSingle("la vente n'a pas été enregistrée");
    }

    [Fact]
    public async Task Une_caisse_vide_ne_s_encaisse_pas()
    {
        var caisse = Caisse();
        await caisse.ChargerAsync();

        await caisse.EnregistrerCommand.ExecuteAsync(null);

        caisse.MessageErreur.Should().Contain("produit");
    }

    [Fact]
    public async Task Encaisser_enregistre_la_vente_et_diminue_le_stock()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = produitId;
        caisse.Quantite = 3m;
        caisse.AjouterLigneCommand.Execute(null);
        caisse.MontantPaye = 15000m;

        _dialogue.ReponseConfirmation = false;   // ne pas imprimer le reçu

        await caisse.EnregistrerCommand.ExecuteAsync(null);

        caisse.MessageErreur.Should().BeNull();

        (await _atelier.StockProduitAsync(produitId)).Should().Be(7m);

        var ventes = await _atelier.Ventes.ListerAsync(new FiltreVentesRequete());
        ventes.Total.Should().Be(1);
        ventes.Elements[0].Total.Should().Be(12600m);
        ventes.Elements[0].Reste.Should().Be(0m);
    }

    [Fact]
    public async Task La_caisse_se_vide_apres_l_encaissement()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = produitId;
        caisse.AjouterLigneCommand.Execute(null);
        caisse.MontantPaye = 5000m;

        _dialogue.ReponseConfirmation = false;

        await caisse.EnregistrerCommand.ExecuteAsync(null);

        // Le caissier enchaîne : rien de la vente précédente ne doit rester.
        caisse.Lignes.Should().BeEmpty();
        caisse.MontantPaye.Should().Be(0m);
        caisse.RemiseDocument.Should().Be(0m);
    }

    [Fact]
    public async Task Une_piece_unique_vendue_disparait_de_la_caisse()
    {
        var piece = await _atelier.CreerProduitAsync("Jarre signée", prixVente: 25000m,
            stockInitial: 1, pieceUnique: true);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = piece;
        caisse.AjouterLigneCommand.Execute(null);
        caisse.MontantPaye = 25000m;
        _dialogue.ReponseConfirmation = false;

        await caisse.EnregistrerCommand.ExecuteAsync(null);
        caisse.MessageErreur.Should().BeNull();

        await caisse.ChargerAsync();

        // Elle n'existe qu'une fois : on ne peut plus la proposer.
        caisse.Articles.Should().NotContain(a => a.Valeur == piece);
        (await _atelier.StockProduitAsync(piece)).Should().Be(0m);
    }

    [Fact]
    public async Task Une_piece_unique_ne_se_vend_pas_en_deux_exemplaires()
    {
        var piece = await _atelier.CreerProduitAsync("Jarre signée", prixVente: 25000m,
            stockInitial: 1, pieceUnique: true);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = piece;
        caisse.Quantite = 2m;
        caisse.AjouterLigneCommand.Execute(null);
        caisse.MontantPaye = caisse.Total;

        await caisse.EnregistrerCommand.ExecuteAsync(null);

        caisse.MessageErreur.Should().Contain("Jarre signée");
        (await _atelier.StockProduitAsync(piece)).Should().Be(1m);
        (await _atelier.Ventes.ListerAsync(new FiltreVentesRequete())).Total.Should().Be(0);
    }

    [Fact]
    public async Task Un_produit_en_rupture_n_est_pas_vendu_sans_stock()
    {
        var produitId = await _atelier.CreerProduitAsync("Bol vert", prixVente: 1200m, stockInitial: 1);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = produitId;
        caisse.Quantite = 3m;
        caisse.AjouterLigneCommand.Execute(null);
        caisse.MontantPaye = caisse.Total;

        await caisse.EnregistrerCommand.ExecuteAsync(null);

        caisse.MessageErreur.Should().Contain("Stock insuffisant");
        (await _atelier.StockProduitAsync(produitId)).Should().Be(1m);
    }

    [Fact]
    public async Task La_remise_globale_diminue_le_total()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = produitId;
        caisse.AjouterLigneCommand.Execute(null);
        caisse.RemiseDocument = 200m;

        caisse.SousTotal.Should().Be(4200m);
        caisse.Total.Should().Be(4000m);
    }

    [Fact]
    public async Task Retirer_une_ligne_met_le_total_a_jour()
    {
        var premier = await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);
        var second = await _atelier.CreerProduitAsync("Bol vert", prixVente: 1200m, stockInitial: 10);

        var caisse = Caisse();
        await caisse.ChargerAsync();

        caisse.ArticleChoisi = premier;
        caisse.AjouterLigneCommand.Execute(null);
        caisse.ArticleChoisi = second;
        caisse.AjouterLigneCommand.Execute(null);

        caisse.LigneSelectionnee = caisse.Lignes[0];
        caisse.RetirerLigneCommand.Execute(null);

        caisse.Lignes.Should().ContainSingle();
        caisse.Total.Should().Be(1200m);
    }
}

/// <summary>Un seul mode de règlement : c'est tout ce dont la caisse a besoin.</summary>
internal sealed class ReferentielMinimal : CeramiPro.Application.Interfaces.IReferentielService
{
    private readonly int _modeId;

    public ReferentielMinimal(int modeId) => _modeId = modeId;

    public Task<IReadOnlyList<CeramiPro.Application.DTOs.Referentiels.ModeReglementDto>>
        ListerModesReglementAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CeramiPro.Application.DTOs.Referentiels.ModeReglementDto>>(
            new[] { new CeramiPro.Application.DTOs.Referentiels.ModeReglementDto(
                _modeId, "especes", "Espèces", false, true) });

    public Task<IReadOnlyList<CeramiPro.Application.DTOs.Referentiels.ElementReferentielDto>> ListerAsync(
        CeramiPro.Application.DTOs.Referentiels.TypeReferentiel type,
        bool inclureInactifs = true, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CeramiPro.Application.DTOs.Referentiels.ElementReferentielDto>>(
            Array.Empty<CeramiPro.Application.DTOs.Referentiels.ElementReferentielDto>());

    public Task<CeramiPro.Application.DTOs.Referentiels.ElementReferentielDto> CreerAsync(
        CeramiPro.Application.DTOs.Referentiels.TypeReferentiel type,
        CeramiPro.Application.DTOs.Referentiels.ElementReferentielRequete requete,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CeramiPro.Application.DTOs.Referentiels.ElementReferentielDto> ModifierAsync(
        CeramiPro.Application.DTOs.Referentiels.TypeReferentiel type, int id,
        CeramiPro.Application.DTOs.Referentiels.ElementReferentielRequete requete,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task SupprimerAsync(
        CeramiPro.Application.DTOs.Referentiels.TypeReferentiel type, int id,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}

/// <summary>Documents simulés : la production des PDF a ses propres tests.</summary>
internal sealed class DocumentMinimal : CeramiPro.Application.Interfaces.IDocumentService
{
    public Task<byte[]> RecuVentePdfAsync(int venteId, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<byte>());

    public Task<byte[]> RecuCommandePdfAsync(int commandeId, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<byte>());
}
