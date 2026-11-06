using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Encaissements reçus des clients : paiements à la caisse, acomptes et
/// soldes de commandes, règlements de ventes à crédit.
/// </summary>
public partial class PaiementsVueModele : ListeVueModele<PaiementDto>
{
    private readonly IPaiementService _service;
    private readonly IDocumentService _documents;

    public PaiementsVueModele(IPaiementService service, IDocumentService documents, OutilsListe outils)
        : base(outils)
    {
        _service = service;
        _documents = documents;
    }

    protected override Type TypeFormulaire => typeof(PaiementFormulaireVueModele);

    /// <summary>Un paiement se corrige par une annulation tracée, jamais par une modification.</summary>
    public override bool PeutModifier => false;

    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("🧾  Reçu", ImprimerRecuCommand,
            Aide: "Reçu de la vente ou de la commande que ce paiement règle."),
        new("Annuler le paiement", AnnulerCommand, Destructive: true,
            Aide: "Le reste à payer de la vente ou de la commande est corrigé.")
    };

    [RelayCommand]
    private Task ImprimerRecuAsync()
    {
        if (ElementSelectionne is { VenteId: null, CommandeId: null })
        {
            Outils?.Dialogue.Information("Ce paiement n'est rattaché à aucune vente ni commande.");
            return Task.CompletedTask;
        }

        return ImprimerAsync(
            paiement => paiement.CommandeId is { } commande
                ? _documents.RecuCommandePdfAsync(commande)
                : _documents.RecuVentePdfAsync(paiement.VenteId!.Value),
            paiement => $"recu-{paiement.CommandeNumero ?? paiement.VenteNumero}.pdf");
    }

    [RelayCommand]
    private Task AnnulerAsync() => AgirAvecMotifAsync(
        (paiement, motif) => _service.AnnulerAsync(paiement.Id, motif),
        question: "Pourquoi annuler ce paiement ?\n\nLe montant redeviendra dû sur la vente ou la commande. " +
                  "Le paiement reste consultable dans l'historique, avec ce motif.",
        succes: "Le paiement est annulé et le reste à payer a été corrigé.");

    public override string Titre => "Paiements";

    public override string Introduction =>
        "Encaissements reçus des clients : ventes, acomptes et soldes des commandes. " +
        "« Ajouter » enregistre un règlement sur une vente à crédit ou une commande.";

    protected override Task<PagedResult<PaiementDto>> LireAsync()
        => _service.ListerAsync(new FiltrePaiementsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Date", "Date", ColonneAlignement.Gauche, FormatColonne.Date),
        new("Client", "ClientNom", ColonneAlignement.Gauche),
        new("Vente", "VenteNumero", ColonneAlignement.Gauche),
        new("Commande", "CommandeNumero", ColonneAlignement.Gauche),
        new("Montant", "Montant", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Mode", "ModeReglement", ColonneAlignement.Gauche),
        new("Acompte", "Acompte", ColonneAlignement.Centre),
        new("Encaissé par", "Utilisateur", ColonneAlignement.Gauche)
    };
}
