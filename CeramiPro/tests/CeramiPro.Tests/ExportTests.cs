using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Infrastructure.Services;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Ecrans;
using CeramiPro.Tests.Aides;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Tests;

/// <summary>
/// Les exports produisent de vrais fichiers : un classeur qu'Excel sait
/// ouvrir, un PDF qu'un lecteur sait afficher. Un fichier texte renommé
/// « .xlsx » passerait tous les tests d'apparence et échouerait chez
/// l'utilisateur — ces vérifications relisent donc ce qui a été écrit.
/// </summary>
public class ExportTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    public ExportTests() => _atelier.AccorderTousLesDroits();

    public void Dispose() => _atelier.Dispose();

    private IExportService Exports() => new ExportService(
        _atelier.Rapports, _atelier.Parametres, _atelier.Horloge);

    private async Task PreparerVentesAsync()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 20);
        var clientId = await _atelier.CreerClientAsync();

        await _atelier.Ventes.EnregistrerAsync(new Application.DTOs.Commercial.VenteRequete
        {
            ClientId = clientId,
            Date = _atelier.Horloge.MaintenantUtc,
            Lignes = new List<Application.DTOs.Commercial.LigneVenteRequete>
            {
                new() { ProduitId = produitId, Quantite = 2m, PrixUnitaire = 4200m }
            }
        });
    }

    [Fact]
    public async Task Le_classeur_produit_est_un_vrai_fichier_Excel()
    {
        await PreparerVentesAsync();

        var (nomFichier, contenu) = await Exports().ExcelAsync(new RapportRequete
        {
            Type = TypeRapport.ChiffreAffaires,
            Du = _atelier.Horloge.Aujourdhui.AddDays(-7),
            Au = _atelier.Horloge.Aujourdhui
        });

        nomFichier.Should().EndWith(".xlsx");
        contenu.Should().NotBeEmpty();

        // Relire le classeur est la seule preuve qu'il s'ouvrira ailleurs.
        using var flux = new MemoryStream(contenu);
        using var classeur = new XLWorkbook(flux);

        classeur.Worksheets.Should().ContainSingle();

        var feuille = classeur.Worksheets.First();
        feuille.Cell(1, 1).GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Le_document_produit_est_un_vrai_PDF()
    {
        await PreparerVentesAsync();

        var (nomFichier, contenu) = await Exports().PdfAsync(new RapportRequete
        {
            Type = TypeRapport.ChiffreAffaires,
            Du = _atelier.Horloge.Aujourdhui.AddDays(-7),
            Au = _atelier.Horloge.Aujourdhui
        });

        nomFichier.Should().EndWith(".pdf");

        // Tout PDF commence par cette signature.
        System.Text.Encoding.ASCII.GetString(contenu, 0, 5).Should().Be("%PDF-");
        contenu.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task Un_tableau_quelconque_s_exporte_aussi()
    {
        var colonnes = new[] { "Produit", "Quantité", "Total" };
        var lignes = new IReadOnlyList<string>[]
        {
            new[] { "Vase bleu", "3", "12 600,00 DA" },
            new[] { "Bol vert", "5", "6 000,00 DA" }
        };

        var (nomFichier, contenu) = await Exports().TableauAsync(
            "Ventes du mois", colonnes, lignes, FormatExport.Excel);

        nomFichier.Should().StartWith("ventes-du-mois");

        using var flux = new MemoryStream(contenu);
        using var classeur = new XLWorkbook(flux);
        var feuille = classeur.Worksheets.First();

        // Titre, période, ligne vide, puis les en-têtes.
        feuille.Cell(4, 1).GetString().Should().Be("Produit");
        feuille.Cell(5, 1).GetString().Should().Be("Vase bleu");
        feuille.Cell(6, 3).GetString().Should().Be("6 000,00 DA");
    }

    [Fact]
    public async Task Le_nom_du_fichier_ne_contient_aucun_caractere_interdit()
    {
        var (nomFichier, _) = await Exports().TableauAsync(
            "Dettes clients : au 31/08/2026", new[] { "Client" },
            new IReadOnlyList<string>[] { new[] { "Mohamed" } }, FormatExport.Excel);

        nomFichier.Should().NotContainAny(Path.GetInvalidFileNameChars().Select(c => c.ToString()));
    }

    [Fact]
    public async Task Exporter_une_liste_depuis_un_ecran_ecrit_le_fichier_choisi()
    {
        await _atelier.CreerClientAsync("Mohamed Benali");
        await _atelier.CreerClientAsync("Amina Cherif");

        var chemin = Path.Combine(Path.GetTempPath(), $"ceramipro-{Guid.NewGuid():N}.xlsx");
        var fichiers = new FichierFactice { CheminChoisi = chemin };
        var dialogue = new DialogueFactice { ReponseConfirmation = false };

        var outils = new OutilsListe(
            new FormulaireFactice(), dialogue, fichiers, Exports(),
            new ServiceCollection().BuildServiceProvider());

        var ecran = new ClientsVueModele(_atelier.Clients, new ServiceLangue(), outils);
        await ecran.ChargerAsync();

        try
        {
            await ecran.ExporterExcelCommand.ExecuteAsync(null);

            ecran.MessageErreur.Should().BeNull();
            File.Exists(chemin).Should().BeTrue();

            using var classeur = new XLWorkbook(chemin);
            var feuille = classeur.Worksheets.First();

            feuille.Cell(4, 2).GetString().Should().Be("Nom");

            // Les deux clients figurent, dans l'ordre où l'écran les affiche.
            var noms = new[] { feuille.Cell(5, 2).GetString(), feuille.Cell(6, 2).GetString() };
            noms.Should().BeEquivalentTo(new[] { "Mohamed Benali", "Amina Cherif" });
        }
        finally
        {
            if (File.Exists(chemin))
            {
                File.Delete(chemin);
            }
        }
    }

    [Fact]
    public async Task Renoncer_a_l_enregistrement_n_ecrit_aucun_fichier()
    {
        await _atelier.CreerClientAsync();

        var fichiers = new FichierFactice { CheminChoisi = null };

        var outils = new OutilsListe(
            new FormulaireFactice(), new DialogueFactice(), fichiers, Exports(),
            new ServiceCollection().BuildServiceProvider());

        var ecran = new ClientsVueModele(_atelier.Clients, new ServiceLangue(), outils);
        await ecran.ChargerAsync();

        await ecran.ExporterExcelCommand.ExecuteAsync(null);

        ecran.MessageErreur.Should().BeNull();
        fichiers.Ouverts.Should().BeEmpty();
    }

    [Fact]
    public async Task Exporter_une_liste_vide_le_dit_plutot_que_d_ecrire_un_fichier_creux()
    {
        var fichiers = new FichierFactice();
        var dialogue = new DialogueFactice();

        var outils = new OutilsListe(
            new FormulaireFactice(), dialogue, fichiers, Exports(),
            new ServiceCollection().BuildServiceProvider());

        var ecran = new ClientsVueModele(_atelier.Clients, new ServiceLangue(), outils);
        await ecran.ChargerAsync();

        await ecran.ExporterExcelCommand.ExecuteAsync(null);

        dialogue.Messages.Should().Contain(m => m.Message.Contains("rien à exporter"));
        fichiers.Demandes.Should().BeEmpty();
    }

    [Fact]
    public async Task L_ecran_des_rapports_propose_les_douze_rapports()
    {
        var ecran = new RapportsVueModele(
            _atelier.Rapports, Exports(), new ServiceLangue(),
            new FichierFactice(), new DialogueFactice());

        ecran.RapportsDisponibles.Should().HaveCount(12);
        ecran.RapportsDisponibles.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.Libelle));

        // La période proposée est le mois en cours : c'est celle que l'on
        // consulte le plus souvent.
        ecran.Du.Day.Should().Be(1);
        ecran.Au.Should().Be(DateTime.Today);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Afficher_un_rapport_remplit_le_tableau()
    {
        await PreparerVentesAsync();

        var ecran = new RapportsVueModele(
            _atelier.Rapports, Exports(), new ServiceLangue(),
            new FichierFactice(), new DialogueFactice());

        ecran.Du = _atelier.Horloge.Aujourdhui.AddDays(-7);
        ecran.Au = _atelier.Horloge.Aujourdhui;

        await ecran.AfficherCommand.ExecuteAsync(null);

        ecran.MessageErreur.Should().BeNull();
        ecran.RapportAffiche.Should().BeTrue();
        ecran.Colonnes.Should().NotBeEmpty();
        ecran.TitreRapport.Should().NotBeNullOrWhiteSpace();
    }
}
