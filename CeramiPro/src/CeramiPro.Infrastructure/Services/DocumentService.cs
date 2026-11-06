using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.DTOs.Settings;
using CeramiPro.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Fabrique les reçus remis au client, au format d'un rouleau de caisse de
/// 80 mm.
///
/// L'atelier ne délivre plus de factures : un reçu suffit, et il porte les
/// mentions légales saisies dans les paramètres — registre du commerce,
/// identifiant fiscal, article d'imposition — quand elles sont renseignées.
/// </summary>
public class DocumentService : IDocumentService
{
    /// <summary>Largeur du rouleau de caisse.</summary>
    private const float LargeurRouleau = 80;

    /// <summary>Largeur de la colonne des montants, en points.</summary>
    private const float ColonneMontant = 70;

    private readonly IVenteService _ventes;
    private readonly ICommandeService _commandes;
    private readonly IPaiementService _paiements;
    private readonly IParametresService _parametres;
    private readonly IServiceDateHeure _horloge;

    public DocumentService(
        IVenteService ventes,
        ICommandeService commandes,
        IPaiementService paiements,
        IParametresService parametres,
        IServiceDateHeure horloge)
    {
        _ventes = ventes;
        _commandes = commandes;
        _paiements = paiements;
        _parametres = parametres;
        _horloge = horloge;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> RecuVentePdfAsync(int venteId, CancellationToken cancellationToken = default)
    {
        var vente = await _ventes.ObtenirAsync(venteId, cancellationToken);
        var atelier = await _parametres.ObtenirAsync(cancellationToken);

        return Rouleau(atelier, colonne =>
        {
            colonne.Item().Text($"Reçu de vente n° {vente.Numero}").Bold();
            colonne.Item().Text(Formatage.DateHeure(vente.Date)).FontSize(7);

            if (!string.IsNullOrWhiteSpace(vente.ClientNom))
            {
                colonne.Item().Text($"Client : {vente.ClientNom}").FontSize(7);
            }

            if (!string.IsNullOrWhiteSpace(vente.Utilisateur))
            {
                colonne.Item().Text($"Servi par : {vente.Utilisateur}").FontSize(7);
            }

            Separateur(colonne);

            foreach (var ligne in vente.Lignes)
            {
                colonne.Item().Text(ligne.ProduitNom).FontSize(8);
                colonne.Item().Row(rangee =>
                {
                    rangee.RelativeItem().Text(
                        $"{Formatage.Quantite(ligne.Quantite)} × {Formatage.Montant(ligne.PrixUnitaire)}")
                        .FontSize(7);
                    rangee.ConstantItem(ColonneMontant).AlignRight()
                        .Text(Formatage.Montant(ligne.Total)).FontSize(8);
                });

                if (ligne.Remise > 0)
                {
                    colonne.Item().Text($"Remise : -{Formatage.Montant(ligne.Remise)}").FontSize(7);
                }
            }

            Separateur(colonne);

            if (vente.Remise > 0 || vente.Tva > 0)
            {
                LigneMontant(colonne, "Sous-total", vente.SousTotal);
            }

            if (vente.Remise > 0)
            {
                LigneMontant(colonne, "Remise", -vente.Remise);
            }

            if (vente.Tva > 0)
            {
                LigneMontant(colonne, "TVA", vente.Tva);
            }

            LigneMontant(colonne, "Total", vente.Total, gras: true);
            LigneMontant(colonne, "Payé", vente.Paye);

            if (vente.Reste > 0)
            {
                LigneMontant(colonne, "Reste à payer", vente.Reste, gras: true);
            }

            if (vente.Statut == Domain.Enums.SaleStatus.Annulee)
            {
                colonne.Item().PaddingTop(4).AlignCenter().Text("VENTE ANNULÉE").Bold().FontSize(10);
            }
        });
    }

    public async Task<byte[]> RecuCommandePdfAsync(int commandeId, CancellationToken cancellationToken = default)
    {
        var commande = await _commandes.ObtenirAsync(commandeId, cancellationToken);
        var atelier = await _parametres.ObtenirAsync(cancellationToken);

        var paiements = await _paiements.ListerAsync(
            new FiltrePaiementsRequete { CommandeId = commandeId, TaillePage = 200 },
            cancellationToken);

        return Rouleau(atelier, colonne =>
        {
            colonne.Item().Text($"Reçu de commande n° {commande.Numero}").Bold();
            colonne.Item().Text($"Édité le {Formatage.DateHeure(_horloge.MaintenantAtelier)}").FontSize(7);
            colonne.Item().Text($"Client : {commande.ClientNom}").FontSize(7);

            if (!string.IsNullOrWhiteSpace(commande.ClientTelephone))
            {
                colonne.Item().Text($"Téléphone : {commande.ClientTelephone}").FontSize(7);
            }

            Separateur(colonne);

            colonne.Item().Text(commande.Titre).Bold().FontSize(8);
            colonne.Item().Text($"Commandée le {Formatage.Date(commande.DateCommande)}").FontSize(7);
            colonne.Item().Text($"À livrer le {Formatage.Date(commande.DateLimite)}").FontSize(7);
            colonne.Item().Text($"Statut : {commande.StatutLibelle}").FontSize(7);

            colonne.Item().Row(rangee =>
            {
                rangee.RelativeItem().Text(
                    $"{Formatage.Quantite(commande.Quantite)} × {Formatage.Montant(commande.PrixUnitaire)}")
                    .FontSize(7);
                rangee.ConstantItem(ColonneMontant).AlignRight()
                    .Text(Formatage.Montant(commande.Quantite * commande.PrixUnitaire)).FontSize(8);
            });

            Separateur(colonne);

            if (commande.Remise > 0)
            {
                LigneMontant(colonne, "Remise", -commande.Remise);
            }

            LigneMontant(colonne, "Total de la commande", commande.Total, gras: true);

            if (paiements.Elements.Count > 0)
            {
                Separateur(colonne);
                colonne.Item().Text("Versements reçus").Bold().FontSize(8);

                foreach (var paiement in paiements.Elements.OrderBy(p => p.Date))
                {
                    var nature = paiement.Acompte ? "Acompte" : "Versement";
                    LigneMontant(colonne,
                        $"{Formatage.Date(paiement.Date)} · {nature} · {paiement.ModeReglement}",
                        paiement.Montant);
                }
            }

            Separateur(colonne);

            LigneMontant(colonne, "Total versé", commande.Paye);
            LigneMontant(colonne, "Reste à payer", commande.Reste, gras: true);

            if (commande.Reste <= 0)
            {
                colonne.Item().PaddingTop(4).AlignCenter().Text("COMMANDE SOLDÉE").Bold().FontSize(9);
            }
        });
    }

    // ------------------------------------------------------------ Mise en page

    /// <summary>
    /// Gabarit commun : en-tête de l'atelier, contenu propre au reçu, puis
    /// mentions légales et remerciement.
    /// </summary>
    private static byte[] Rouleau(ParametresAtelierDto atelier, Action<ColumnDescriptor> contenu)
        => Document.Create(document => document.Page(page =>
        {
            // Rouleau de caisse : largeur fixe, hauteur libre.
            page.ContinuousSize(LargeurRouleau, Unit.Millimetre);
            page.Margin(5, Unit.Millimetre);
            page.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Black).FontFamily("Arial"));

            page.Content().Column(colonne =>
            {
                colonne.Spacing(3);

                Entete(colonne, atelier);
                Separateur(colonne);

                contenu(colonne);

                PiedDeRecu(colonne, atelier);
            });
        })).GeneratePdf();

    private static void Entete(ColumnDescriptor colonne, ParametresAtelierDto atelier)
    {
        colonne.Item().AlignCenter().Text(atelier.NomAtelier).Bold().FontSize(11);

        foreach (var ligne in new[] { atelier.Adresse, atelier.Ville, atelier.Telephone }
                     .Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            colonne.Item().AlignCenter().Text(ligne!).FontSize(7);
        }
    }

    private static void PiedDeRecu(ColumnDescriptor colonne, ParametresAtelierDto atelier)
    {
        var mentions = new[]
        {
            Mention("RC", atelier.RegistreCommerce),
            Mention("NIF", atelier.NumeroIdentificationFiscale),
            Mention("Art.", atelier.ArticleImposition)
        }.Where(m => m is not null).ToList();

        if (mentions.Count > 0)
        {
            Separateur(colonne);

            foreach (var mention in mentions)
            {
                colonne.Item().AlignCenter().Text(mention!).FontSize(6.5f);
            }
        }

        var remerciement = string.IsNullOrWhiteSpace(atelier.PiedDePageRecu)
            ? "Merci de votre confiance"
            : atelier.PiedDePageRecu;

        colonne.Item().PaddingTop(8).AlignCenter().Text(remerciement).FontSize(8).Italic();
    }

    private static void Separateur(ColumnDescriptor colonne)
        => colonne.Item().PaddingVertical(4).LineHorizontal(0.5f);

    private static void LigneMontant(ColumnDescriptor colonne, string libelle, decimal montant, bool gras = false)
        => colonne.Item().Row(rangee =>
        {
            var etiquette = rangee.RelativeItem().Text(libelle).FontSize(gras ? 8.5f : 7.5f);
            var valeur = rangee.ConstantItem(ColonneMontant).AlignRight()
                .Text(Formatage.Montant(montant)).FontSize(gras ? 8.5f : 7.5f);

            if (gras)
            {
                etiquette.Bold();
                valeur.Bold();
            }
        });

    private static string? Mention(string etiquette, string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : $"{etiquette} : {valeur}";
}
