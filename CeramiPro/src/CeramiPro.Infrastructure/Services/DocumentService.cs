using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Codes;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.DTOs.Settings;
using CeramiPro.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Fabrique les factures et les reçus au format PDF.
///
/// Les documents portent l'identité de l'atelier telle qu'elle est saisie
/// dans les paramètres — nom, adresse, registre de commerce, identifiant
/// fiscal — car ces mentions sont attendues sur une facture algérienne.
/// </summary>
public class DocumentService : IDocumentService
{
    /// <summary>Teinte de l'atelier, reprise sur les documents imprimés.</summary>
    private const string Terre = "#A45A3C";
    private const string Ardoise = "#1F2933";
    private const string GrisLeger = "#F4F6F8";

    private readonly IFactureService _factures;
    private readonly IVenteService _ventes;
    private readonly IParametresService _parametres;
    private readonly IServiceDateHeure _horloge;
    private readonly string _dossierDocuments;

    public DocumentService(
        IFactureService factures,
        IVenteService ventes,
        IParametresService parametres,
        IServiceDateHeure horloge,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _factures = factures;
        _ventes = ventes;
        _parametres = parametres;
        _horloge = horloge;

        _dossierDocuments = configuration["Documents:Dossier"]
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CeramiPro", "documents");

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> FacturePdfAsync(int factureId, CancellationToken cancellationToken = default)
    {
        var facture = await _factures.ObtenirAsync(factureId, cancellationToken);
        var atelier = await _parametres.ObtenirAsync(cancellationToken);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(10).FontColor(Ardoise).FontFamily("Arial"));

            page.Header().Element(entete => EnteteAtelier(entete, atelier, facture));
            page.Content().Element(contenu => CorpsFacture(contenu, facture));
            page.Footer().Element(pied => PiedDePage(pied, atelier));
        })).GeneratePdf();
    }

    public async Task<byte[]> RecuPdfAsync(int venteId, CancellationToken cancellationToken = default)
    {
        var vente = await _ventes.ObtenirAsync(venteId, cancellationToken);
        var atelier = await _parametres.ObtenirAsync(cancellationToken);

        return Document.Create(document => document.Page(page =>
        {
            // Rouleau de caisse : 80 mm de large, hauteur libre.
            page.ContinuousSize(80, Unit.Millimetre);
            page.Margin(5, Unit.Millimetre);
            page.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Black).FontFamily("Arial"));

            page.Content().Column(colonne =>
            {
                colonne.Spacing(3);

                colonne.Item().AlignCenter().Text(atelier.NomAtelier).Bold().FontSize(11);

                if (!string.IsNullOrWhiteSpace(atelier.Telephone))
                {
                    colonne.Item().AlignCenter().Text(atelier.Telephone).FontSize(7);
                }

                colonne.Item().PaddingVertical(4).LineHorizontal(0.5f);

                colonne.Item().Text($"Reçu n° {vente.Numero}").Bold();
                colonne.Item().Text(Formatage.DateHeure(vente.Date)).FontSize(7);

                if (!string.IsNullOrWhiteSpace(vente.ClientNom))
                {
                    colonne.Item().Text($"Client : {vente.ClientNom}").FontSize(7);
                }

                colonne.Item().PaddingVertical(4).LineHorizontal(0.5f);

                foreach (var ligne in vente.Lignes)
                {
                    colonne.Item().Text(ligne.ProduitNom).FontSize(8);
                    colonne.Item().Row(rangee =>
                    {
                        rangee.RelativeItem().Text(
                            $"{Formatage.Quantite(ligne.Quantite)} × {Formatage.Montant(ligne.PrixUnitaire)}")
                            .FontSize(7);
                        rangee.ConstantItem(70).AlignRight().Text(Formatage.Montant(ligne.Total)).FontSize(8);
                    });
                }

                colonne.Item().PaddingVertical(4).LineHorizontal(0.5f);

                LigneTotal(colonne, "Total", vente.Total, gras: true);
                LigneTotal(colonne, "Payé", vente.Paye);

                if (vente.Reste > 0)
                {
                    LigneTotal(colonne, "Reste à payer", vente.Reste, gras: true);
                }

                colonne.Item().PaddingTop(8).AlignCenter()
                    .Text("Merci de votre confiance").FontSize(8).Italic();
            });
        })).GeneratePdf();
    }

    public async Task<string> EnregistrerFactureAsync(
        int factureId, CancellationToken cancellationToken = default)
    {
        var facture = await _factures.ObtenirAsync(factureId, cancellationToken);
        var contenu = await FacturePdfAsync(factureId, cancellationToken);

        Directory.CreateDirectory(_dossierDocuments);

        var chemin = Path.Combine(_dossierDocuments, $"{facture.Numero}.pdf");
        await File.WriteAllBytesAsync(chemin, contenu, cancellationToken);

        return chemin;
    }

    // ------------------------------------------------------------- Mise en page

    private static void EnteteAtelier(IContainer conteneur, ParametresAtelierDto atelier, FactureDto facture)
        => conteneur.Row(rangee =>
        {
            rangee.RelativeItem().Column(colonne =>
            {
                colonne.Item().Text(atelier.NomAtelier).Bold().FontSize(18).FontColor(Terre);

                foreach (var ligne in new[]
                         {
                             atelier.RaisonSociale, atelier.Adresse, atelier.Ville,
                             atelier.Telephone, atelier.Email
                         }.Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    colonne.Item().Text(ligne).FontSize(9);
                }

                foreach (var mention in new[]
                         {
                             Mention("RC", atelier.RegistreCommerce),
                             Mention("NIF", atelier.NumeroIdentificationFiscale),
                             Mention("Art. imposition", atelier.ArticleImposition)
                         }.Where(m => m is not null))
                {
                    colonne.Item().Text(mention).FontSize(8).FontColor(Colors.Grey.Darken1);
                }
            });

            rangee.ConstantItem(190).Column(colonne =>
            {
                colonne.Item().AlignRight().Text("FACTURE").Bold().FontSize(20).FontColor(Terre);
                colonne.Item().AlignRight().Text(facture.Numero).Bold().FontSize(12);
                colonne.Item().AlignRight().Text($"Date : {Formatage.Date(facture.DateEmission)}").FontSize(9);

                if (facture.DateEcheance is { } echeance)
                {
                    colonne.Item().AlignRight().Text($"Échéance : {Formatage.Date(echeance)}").FontSize(9);
                }

                colonne.Item().PaddingTop(6).AlignRight()
                    .Text(facture.StatutLibelle).FontSize(9).Bold();
            });
        });

    private static void CorpsFacture(IContainer conteneur, FactureDto facture)
        => conteneur.PaddingVertical(20).Column(colonne =>
        {
            colonne.Spacing(14);

            // Destinataire
            colonne.Item().Background(GrisLeger).Padding(10).Column(client =>
            {
                client.Item().Text("Facturé à").FontSize(8).FontColor(Colors.Grey.Darken1);
                client.Item().Text(facture.ClientNom).Bold().FontSize(12);
            });

            // Lignes
            colonne.Item().Table(tableau =>
            {
                tableau.ColumnsDefinition(colonnes =>
                {
                    colonnes.RelativeColumn(4);
                    colonnes.ConstantColumn(60);
                    colonnes.ConstantColumn(85);
                    colonnes.ConstantColumn(85);
                });

                tableau.Header(entete =>
                {
                    foreach (var (titre, aDroite) in new[]
                             {
                                 ("Désignation", false), ("Quantité", true),
                                 ("Prix unitaire", true), ("Total", true)
                             })
                    {
                        var cellule = entete.Cell().Background(Ardoise).Padding(7);

                        // L'alignement se pose avant le texte : une cellule
                        // ne reçoit qu'un seul enfant.
                        var contenu = aDroite ? cellule.AlignRight() : cellule;

                        contenu.Text(titre).FontColor(Colors.White).Bold().FontSize(9);
                    }
                });

                foreach (var ligne in facture.Lignes)
                {
                    tableau.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(7).Text(ligne.Description);
                    tableau.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(7).AlignRight().Text(Formatage.Quantite(ligne.Quantite));
                    tableau.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(7).AlignRight().Text(Formatage.Montant(ligne.PrixUnitaire));
                    tableau.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(7).AlignRight().Text(Formatage.Montant(ligne.Total));
                }
            });

            // Totaux
            colonne.Item().AlignRight().Width(260).Column(totaux =>
            {
                LigneMontant(totaux, "Sous-total", facture.SousTotal);

                if (facture.Remise > 0)
                {
                    LigneMontant(totaux, "Remise", -facture.Remise);
                }

                if (facture.Tva > 0)
                {
                    LigneMontant(totaux, $"TVA ({Formatage.Pourcentage(facture.TauxTva)})", facture.Tva);
                }

                totaux.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Ardoise);

                LigneMontant(totaux, "Total à payer", facture.Total, gras: true, taille: 13);
                LigneMontant(totaux, "Montant payé", facture.Paye);

                if (facture.Reste > 0)
                {
                    LigneMontant(totaux, "Reste à payer", facture.Reste, gras: true, couleur: Terre);
                }
            });

            if (!string.IsNullOrWhiteSpace(facture.Notes))
            {
                colonne.Item().PaddingTop(10).Text(facture.Notes).FontSize(9).Italic();
            }
        });

    private void PiedDePage(IContainer conteneur, ParametresAtelierDto atelier)
        => conteneur.BorderTop(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingTop(8)
            .Row(rangee =>
            {
                rangee.RelativeItem().Text($"{atelier.NomAtelier} — {Formatage.DateHeure(_horloge.MaintenantAtelier)}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);

                rangee.ConstantItem(90).AlignRight().Text(texte =>
                {
                    texte.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Grey.Darken1));
                    texte.Span("Page ");
                    texte.CurrentPageNumber();
                    texte.Span(" / ");
                    texte.TotalPages();
                });
            });

    private static void LigneMontant(
        ColumnDescriptor colonne, string libelle, decimal montant,
        bool gras = false, float taille = 10, string? couleur = null)
        => colonne.Item().Row(rangee =>
        {
            var etiquette = rangee.RelativeItem().Text(libelle).FontSize(taille);
            var valeur = rangee.ConstantItem(120).AlignRight()
                .Text(Formatage.Montant(montant)).FontSize(taille);

            if (gras)
            {
                etiquette.Bold();
                valeur.Bold();
            }

            if (couleur is not null)
            {
                etiquette.FontColor(couleur);
                valeur.FontColor(couleur);
            }
        });

    private static void LigneTotal(ColumnDescriptor colonne, string libelle, decimal montant, bool gras = false)
        => colonne.Item().Row(rangee =>
        {
            var etiquette = rangee.RelativeItem().Text(libelle).FontSize(8);
            var valeur = rangee.ConstantItem(70).AlignRight().Text(Formatage.Montant(montant)).FontSize(8);

            if (gras)
            {
                etiquette.Bold();
                valeur.Bold();
            }
        });

    private static string? Mention(string etiquette, string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : $"{etiquette} : {valeur}";

    // ------------------------------------------------------------ Étiquettes

    /// <summary>Trois étiquettes par rangée : la taille habituelle du papier autocollant A4.</summary>
    private const int EtiquettesParRangee = 3;

    public async Task<byte[]> EtiquettesPdfAsync(
        IReadOnlyList<EtiquetteDto> etiquettes, CancellationToken cancellationToken = default)
    {
        if (etiquettes.Count == 0)
        {
            throw new RegleMetierException("Aucune étiquette à imprimer.");
        }

        var atelier = await _parametres.ObtenirAsync(cancellationToken);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(8).FontFamily("Arial"));

            page.Header().PaddingBottom(8).Row(rangee =>
            {
                rangee.RelativeItem().Text(atelier.NomAtelier).Bold().FontSize(11).FontColor(Terre);

                rangee.ConstantItem(150).AlignRight()
                    .Text($"{etiquettes.Count} étiquette(s) — {Formatage.Date(_horloge.Aujourdhui)}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });

            page.Content().Table(tableau =>
            {
                tableau.ColumnsDefinition(colonnes =>
                {
                    for (var rang = 0; rang < EtiquettesParRangee; rang++)
                    {
                        colonnes.RelativeColumn();
                    }
                });

                foreach (var etiquette in etiquettes)
                {
                    tableau.Cell().Padding(3).Border(0.75f).BorderColor(Colors.Grey.Medium)
                        .Padding(7).Column(carte =>
                        {
                            carte.Item().Text(etiquette.Nom).Bold().FontSize(9);

                            carte.Item().Text(etiquette.Categorie)
                                .FontSize(7).FontColor(Colors.Grey.Darken1);

                            carte.Item().PaddingTop(3).Text(etiquette.PrixAffiche)
                                .Bold().FontSize(13).FontColor(Terre);

                            carte.Item().PaddingTop(5).Row(codes =>
                            {
                                // Le code-barres sert à la caisse, le QR à
                                // retrouver la fiche depuis un téléphone.
                                if (!string.IsNullOrWhiteSpace(etiquette.CodeBarresSvg))
                                {
                                    codes.RelativeItem().Height(28).Svg(etiquette.CodeBarresSvg);
                                }

                                if (!string.IsNullOrWhiteSpace(etiquette.CodeQrSvg))
                                {
                                    codes.ConstantItem(38).Height(38).Svg(etiquette.CodeQrSvg);
                                }
                            });

                            carte.Item().PaddingTop(2).Text(etiquette.CodeBarres)
                                .FontSize(6.5f).FontColor(Colors.Grey.Darken2);
                        });
                }

                // Compléter la dernière rangée : sans cela, QuestPDF étirerait
                // la dernière étiquette sur toute la largeur restante.
                var manquantes = (EtiquettesParRangee - etiquettes.Count % EtiquettesParRangee)
                    % EtiquettesParRangee;

                for (var rang = 0; rang < manquantes; rang++)
                {
                    tableau.Cell().Padding(3);
                }
            });

            page.Footer().AlignCenter().Text(texte =>
            {
                texte.DefaultTextStyle(t => t.FontSize(7).FontColor(Colors.Grey.Darken1));
                texte.CurrentPageNumber();
                texte.Span(" / ");
                texte.TotalPages();
            });
        })).GeneratePdf();
    }
}
