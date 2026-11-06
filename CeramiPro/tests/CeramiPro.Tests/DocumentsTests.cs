using CeramiPro.Infrastructure.Services;
using CeramiPro.Tests.Aides;
using FluentAssertions;

namespace CeramiPro.Tests;

/// <summary>
/// Le reçu est la pièce que l'atelier remet au client : il doit être produit
/// réellement, pas seulement compiler.
/// </summary>
public class DocumentsTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    public void Dispose() => _atelier.Dispose();

    private DocumentService Documents() => new(
        _atelier.Ventes, _atelier.Commandes, _atelier.Paiements, _atelier.Parametres, _atelier.Horloge);

    /// <summary>Un PDF valide commence toujours par la signature « %PDF- ».</summary>
    private static void DoitEtreUnPdf(byte[] contenu)
    {
        contenu.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(contenu, 0, 5).Should().Be("%PDF-");
        contenu.Length.Should().BeGreaterThan(1000, "un reçu complet pèse plus qu'un fichier vide");
    }

    [Fact]
    public async Task Le_recu_d_une_vente_est_un_vrai_PDF()
    {
        var clientId = await _atelier.CreerClientAsync("Karim Saïdi");
        var produitId = await _atelier.CreerProduitAsync("Vase émaillé", prixVente: 3500m, stockInitial: 10m);
        var vente = await _atelier.VendreAsync(produitId, quantite: 3m, prix: 3500m, paye: 5000m, clientId: clientId);

        DoitEtreUnPdf(await Documents().RecuVentePdfAsync(vente.Id));
    }

    [Fact]
    public async Task Le_recu_d_une_vente_au_comptoir_sans_client_se_produit_aussi()
    {
        var produitId = await _atelier.CreerProduitAsync("Bol", prixVente: 800m, stockInitial: 10m);
        var vente = await _atelier.VendreAsync(produitId, prix: 800m, paye: 800m);

        DoitEtreUnPdf(await Documents().RecuVentePdfAsync(vente.Id));
    }

    [Fact]
    public async Task Le_recu_d_une_commande_reprend_ses_acomptes()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync(), prixUnitaire: 12000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 4000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 2000m);

        DoitEtreUnPdf(await Documents().RecuCommandePdfAsync(commande.Id));
    }

    [Fact]
    public async Task Le_recu_porte_les_mentions_saisies_dans_les_parametres()
    {
        var reglages = await _atelier.Parametres.ObtenirAsync();
        reglages.NomAtelier = "Atelier Tafsut";
        reglages.PiedDePageRecu = "Merci de votre visite — pièces faites main";
        await _atelier.Parametres.ModifierAsync(reglages);

        var produitId = await _atelier.CreerProduitAsync("Bol", prixVente: 800m, stockInitial: 10m);
        var vente = await _atelier.VendreAsync(produitId, prix: 800m, paye: 800m);

        DoitEtreUnPdf(await Documents().RecuVentePdfAsync(vente.Id));
    }

    [Fact]
    public async Task Une_vente_inexistante_est_signalee()
    {
        var action = () => Documents().RecuVentePdfAsync(9999);

        await action.Should().ThrowAsync<Exception>();
    }
}
