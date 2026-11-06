using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Historique des ventes. Une vente ne se modifie pas : on réimprime son
/// reçu, on encaisse ce qui reste dû, ou on l'annule — ce qui remet les
/// produits en stock et laisse une trace.
/// </summary>
public partial class VentesVueModele : ListeVueModele<VenteDto>
{
    private readonly IVenteService _service;
    private readonly IDocumentService _documents;

    public VentesVueModele(IVenteService service, IDocumentService documents, OutilsListe outils)
        : base(outils)
    {
        _service = service;
        _documents = documents;

        Filtres = new FiltreListe[]
        {
            new FiltreCase("Seulement les ventes à crédit (reste à payer)", FiltreChangeAsync)
        };
    }

    public override IReadOnlyList<FiltreListe> Filtres { get; }

    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("🧾  Réimprimer le reçu", ImprimerRecuCommand),
        new("💰  Encaisser le reste", EncaisserCommand,
            Aide: "Pour une vente à crédit : enregistre ce que le client paie maintenant."),
        new("Annuler la vente", AnnulerCommand, Destructive: true,
            Aide: "Les produits vendus retournent en stock ; une vente payée est remboursée.")
    };

    [RelayCommand]
    private Task ImprimerRecuAsync() => ImprimerAsync(
        vente => _documents.RecuVentePdfAsync(vente.Id),
        vente => $"recu-{vente.Numero}.pdf");

    [RelayCommand]
    private Task EncaisserAsync()
    {
        if (ElementSelectionne is not { } vente)
        {
            Outils?.Dialogue.Avertissement("Choisissez d'abord une vente dans le tableau.");
            return Task.CompletedTask;
        }

        if (vente.Statut == SaleStatus.Annulee || vente.Reste <= 0)
        {
            Outils?.Dialogue.Information($"La vente {vente.Numero} n'a rien à encaisser.");
            return Task.CompletedTask;
        }

        return OuvrirFormulaireAsync<PaiementFormulaireVueModele>(formulaire =>
        {
            formulaire.Preselectionner(ReglementRequete.CodeVente(vente.Id), vente.Reste);
            return Task.CompletedTask;
        });
    }

    [RelayCommand]
    private Task AnnulerAsync()
    {
        var vente = ElementSelectionne;
        var payee = vente is { Paye: > 0 };

        return AgirAvecMotifAsync(
            (choisie, motif) => _service.AnnulerAsync(choisie.Id, motif, rembourser: payee),
            question: payee
                ? $"Cette vente a été payée {Formatage.Montant(vente!.Paye)}. L'annuler revient à rembourser " +
                  "le client : ses paiements seront annulés et les produits reviendront en stock.\n\n" +
                  "Pourquoi l'annuler ?"
                : "Pourquoi annuler cette vente ?\n\nLes produits vendus reviendront en stock. " +
                  "Le motif reste dans l'historique.",
            succes: payee
                ? "La vente est annulée : rendez la somme au client. Les produits sont revenus en stock."
                : "La vente est annulée et les produits sont revenus en stock.");
    }

    public override string Titre => "Historique des ventes";

    public override string Introduction =>
        "Toutes les ventes enregistrées. Une nouvelle vente se saisit depuis « Nouvelle vente ».";

    protected override Task<PagedResult<VenteDto>> LireAsync()
        => _service.ListerAsync(new FiltreVentesRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim(),
            SeulementImpayees = ((FiltreCase)Filtres[0]).Actif
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Date", "Date", ColonneAlignement.Gauche, FormatColonne.DateHeure),
        new("Client", "ClientNom", ColonneAlignement.Gauche),
        new("Total", "Total", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Payé", "Paye", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Reste", "Reste", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Bénéfice", "Benefice", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche),
        new("Vendeur", "Utilisateur", ColonneAlignement.Gauche)
    };
}
