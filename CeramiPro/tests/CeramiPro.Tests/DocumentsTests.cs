using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Infrastructure.Services;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CeramiPro.Tests;

/// <summary>
/// La facture est la pièce que l'atelier remet au client : elle doit être
/// produite réellement, pas seulement compiler.
/// </summary>
public class DocumentsTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();
    private readonly string _dossier = Path.Combine(Path.GetTempPath(), "ceramipro-" + Guid.NewGuid());

    private DocumentService Documents()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Documents:Dossier"] = _dossier })
            .Build();

        return new DocumentService(
            _atelier.Factures, _atelier.Ventes, _atelier.Parametres, _atelier.Horloge, configuration);
    }

    private async Task<(int VenteId, int FactureId)> VendreAsync()
    {
        var clientId = await _atelier.CreerClientAsync("Karim Saïdi");
        var produitId = await _atelier.CreerProduitAsync("Vase émaillé", prixVente: 3500m, stockInitial: 10m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            MontantPaye = 5000m,
            ModeReglementId = _atelier.ModeReglementId,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 3m } }
        });

        var facture = (await _atelier.Factures.ListerAsync(new FiltreFacturesRequete()))
            .Elements.First(f => f.VenteId == vente.Id);

        return (vente.Id, facture.Id);
    }

    /// <summary>Un PDF valide commence toujours par la signature « %PDF- ».</summary>
    private static void DoitEtreUnPdf(byte[] contenu)
    {
        contenu.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(contenu, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public async Task Une_facture_produit_un_vrai_fichier_PDF()
    {
        var (_, factureId) = await VendreAsync();

        var contenu = await Documents().FacturePdfAsync(factureId);

        DoitEtreUnPdf(contenu);
        contenu.Length.Should().BeGreaterThan(1000, "une facture complète pèse plus qu'un fichier vide");
    }

    [Fact]
    public async Task Un_recu_de_caisse_produit_un_vrai_fichier_PDF()
    {
        var (venteId, _) = await VendreAsync();

        DoitEtreUnPdf(await Documents().RecuPdfAsync(venteId));
    }

    [Fact]
    public async Task La_facture_est_enregistree_sous_son_numero()
    {
        var (_, factureId) = await VendreAsync();

        var chemin = await Documents().EnregistrerFactureAsync(factureId);

        File.Exists(chemin).Should().BeTrue();
        Path.GetFileName(chemin).Should().StartWith("FAC-").And.EndWith(".pdf");
        DoitEtreUnPdf(await File.ReadAllBytesAsync(chemin));
    }

    [Fact]
    public async Task Une_facture_inexistante_est_signalee()
    {
        var action = async () => await Documents().FacturePdfAsync(9999);

        await action.Should().ThrowAsync<Exception>();
    }

    public void Dispose()
    {
        _atelier.Dispose();

        if (Directory.Exists(_dossier))
        {
            Directory.Delete(_dossier, recursive: true);
        }
    }
}
