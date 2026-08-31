using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Export des rapports et des listes.
///
/// Le format Excel produit un vrai classeur — pas un fichier séparé par des
/// points-virgules renommé — de sorte que les accents, les montants et les
/// dates s'ouvrent correctement sans réglage.
///
/// La mise en page est écrite une seule fois : les douze rapports et les
/// dix-huit écrans de liste en profitent de la même façon.
/// </summary>
public class ExportService : IExportService
{
    private const string Terre = "#A45A3C";
    private const string Ardoise = "#1F2933";

    private readonly IRapportService _rapports;
    private readonly IParametresService _parametres;
    private readonly IServiceDateHeure _horloge;

    public ExportService(
        IRapportService rapports, IParametresService parametres, IServiceDateHeure horloge)
    {
        _rapports = rapports;
        _parametres = parametres;
        _horloge = horloge;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<(string NomFichier, byte[] Contenu)> ExcelAsync(
        RapportRequete requete, CancellationToken cancellationToken = default)
    {
        var rapport = await _rapports.GenererAsync(requete, cancellationToken);

        return (NomFichier(rapport.Titre, "xlsx"),
            Classeur(rapport.Titre, rapport.Periode, rapport.Colonnes, rapport.Lignes, rapport.Totaux));
    }

    public async Task<(string NomFichier, byte[] Contenu)> PdfAsync(
        RapportRequete requete, CancellationToken cancellationToken = default)
    {
        var rapport = await _rapports.GenererAsync(requete, cancellationToken);

        var contenu = await ImprimerAsync(
            rapport.Titre, rapport.Periode, rapport.Colonnes, rapport.Lignes,
            rapport.Totaux, cancellationToken);

        return (NomFichier(rapport.Titre, "pdf"), contenu);
    }

    public async Task<(string NomFichier, byte[] Contenu)> TableauAsync(
        string titre,
        IReadOnlyList<string> colonnes,
        IReadOnlyList<IReadOnlyList<string>> lignes,
        FormatExport format,
        CancellationToken cancellationToken = default)
    {
        var periode = "Édité le " + Formatage.DateHeure(_horloge.MaintenantAtelier);

        if (format == FormatExport.Excel)
        {
            return (NomFichier(titre, "xlsx"), Classeur(titre, periode, colonnes, lignes, null));
        }

        var contenu = await ImprimerAsync(titre, periode, colonnes, lignes, null, cancellationToken);

        return (NomFichier(titre, "pdf"), contenu);
    }

    /// <summary>Classeur Excel : en-têtes lisibles, filtre, ligne figée, totaux.</summary>
    private static byte[] Classeur(
        string titre,
        string periode,
        IReadOnlyList<string> colonnes,
        IReadOnlyList<IReadOnlyList<string>> lignes,
        IReadOnlyList<string>? totaux)
    {
        using var classeur = new XLWorkbook();
        var feuille = classeur.Worksheets.Add(NomFeuille(titre));

        var rang = 1;

        feuille.Cell(rang, 1).Value = titre;
        feuille.Cell(rang, 1).Style.Font.SetBold().Font.SetFontSize(15);
        feuille.Range(rang, 1, rang, Math.Max(1, colonnes.Count)).Merge();
        rang++;

        feuille.Cell(rang, 1).Value = periode;
        feuille.Cell(rang, 1).Style.Font.SetItalic().Font.SetFontColor(XLColor.Gray);
        rang += 2;

        var rangEnTetes = rang;

        for (var colonne = 0; colonne < colonnes.Count; colonne++)
        {
            var cellule = feuille.Cell(rang, colonne + 1);
            cellule.Value = colonnes[colonne];
            cellule.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
            cellule.Style.Fill.SetBackgroundColor(XLColor.FromHtml(Ardoise));
            cellule.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        }

        rang++;

        foreach (var valeurs in lignes)
        {
            for (var colonne = 0; colonne < valeurs.Count; colonne++)
            {
                feuille.Cell(rang, colonne + 1).Value = valeurs[colonne];
            }

            rang++;
        }

        if (totaux is not null)
        {
            for (var colonne = 0; colonne < totaux.Count; colonne++)
            {
                var cellule = feuille.Cell(rang, colonne + 1);
                cellule.Value = totaux[colonne];
                cellule.Style.Font.SetBold();
                cellule.Style.Border.SetTopBorder(XLBorderStyleValues.Thin);
            }
        }

        if (lignes.Count > 0 && colonnes.Count > 0)
        {
            feuille.Range(rangEnTetes, 1, rangEnTetes, colonnes.Count).SetAutoFilter();
            feuille.SheetView.FreezeRows(rangEnTetes);
        }

        feuille.Columns().AdjustToContents();

        using var flux = new MemoryStream();
        classeur.SaveAs(flux);

        return flux.ToArray();
    }

    /// <summary>
    /// Met un tableau en page au format A4 paysage : un tableau large y tient
    /// sans que les colonnes finissent écrasées.
    /// </summary>
    private async Task<byte[]> ImprimerAsync(
        string titre,
        string periode,
        IReadOnlyList<string> colonnes,
        IReadOnlyList<IReadOnlyList<string>> lignes,
        IReadOnlyList<string>? totaux,
        CancellationToken cancellationToken)
    {
        var atelier = await _parametres.ObtenirAsync(cancellationToken);
        var edition = Formatage.DateHeure(_horloge.MaintenantAtelier);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.2f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Arial"));

            page.Header().Column(entete =>
            {
                entete.Item().Text(titre).Bold().FontSize(16).FontColor(Terre);
                entete.Item().Text(periode).FontSize(9).FontColor(Colors.Grey.Darken1);
                entete.Item().PaddingTop(4).Text(atelier.NomAtelier).FontSize(9);
            });

            page.Content().PaddingVertical(14).Table(tableau =>
            {
                tableau.ColumnsDefinition(definition =>
                {
                    foreach (var _ in colonnes)
                    {
                        definition.RelativeColumn();
                    }
                });

                tableau.Header(entete =>
                {
                    foreach (var nom in colonnes)
                    {
                        entete.Cell().Background(Ardoise).Padding(6)
                            .Text(nom).FontColor(Colors.White).Bold().FontSize(9);
                    }
                });

                foreach (var valeurs in lignes)
                {
                    foreach (var valeur in valeurs)
                    {
                        tableau.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(5).Text(valeur).FontSize(9);
                    }
                }

                if (totaux is not null)
                {
                    foreach (var total in totaux)
                    {
                        tableau.Cell().BorderTop(1).BorderColor(Ardoise)
                            .Padding(5).Text(total).Bold().FontSize(9);
                    }
                }
            });

            page.Footer().Row(rangee =>
            {
                rangee.RelativeItem().Text(edition)
                    .FontSize(8).FontColor(Colors.Grey.Darken1);

                rangee.ConstantItem(90).AlignRight().Text(texte =>
                {
                    texte.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Grey.Darken1));
                    texte.CurrentPageNumber();
                    texte.Span(" / ");
                    texte.TotalPages();
                });
            });
        })).GeneratePdf();
    }

    private string NomFichier(string titre, string extension)
        => $"{Nettoyer(titre)}-{_horloge.Aujourdhui:yyyy-MM-dd}.{extension}";

    /// <summary>Un nom d'onglet Excel ne peut pas dépasser 31 caractères.</summary>
    private static string NomFeuille(string titre)
        => titre.Length <= 31 ? titre : titre[..31];

    private static string Nettoyer(string titre)
    {
        var propre = titre.ToLowerInvariant().Replace(' ', '-');

        foreach (var interdit in Path.GetInvalidFileNameChars())
        {
            propre = propre.Replace(interdit, '-');
        }

        return propre.Replace("'", string.Empty);
    }
}
