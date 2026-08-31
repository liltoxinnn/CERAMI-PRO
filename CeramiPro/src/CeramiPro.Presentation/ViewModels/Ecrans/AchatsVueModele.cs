using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CommunityToolkit.Mvvm.Input;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Achats de matières premières et réceptions en stock.</summary>
public partial class AchatsVueModele : ListeVueModele<AchatDto>
{
    private readonly IAchatService _service;

    public AchatsVueModele(IAchatService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;


    /// <summary>
    /// Le parcours d'un achat : brouillon, confirmé, puis réceptionné. Les
    /// matières n'entrent en stock qu'à la réception, quand on a vérifié ce
    /// qui a été livré.
    /// </summary>
    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("Confirmer la commande", ConfirmerCommand,
            Aide: "L'achat passe de brouillon à commande confirmée."),
        new("Enregistrer la réception", ReceptionnerCommand,
            Aide: "Les matières commandées entrent en stock."),
        new("Annuler l'achat", AnnulerCommand, Destructive: true)
    };

    [RelayCommand]
    private Task ConfirmerAsync() => AgirAsync(
        achat => _service.ConfirmerAsync(achat.Id),
        confirmation: "Confirmer cette commande auprès du fournisseur ?",
        succes: "L'achat est confirmé. Enregistrez sa réception à la livraison.");

    /// <summary>
    /// Réceptionne tout ce qui reste à recevoir. C'est le cas courant : une
    /// livraison partielle se saisit ligne par ligne depuis la fiche de
    /// l'achat, celle-ci ne fait qu'éviter la saisie la plus fréquente.
    /// </summary>
    [RelayCommand]
    private Task ReceptionnerAsync() => AgirAsync(
        async achat =>
        {
            var complet = await _service.ObtenirAsync(achat.Id);

            var restantes = complet.Lignes
                .Where(l => l.Quantite > l.QuantiteRecue)
                .Select(l => new LigneReceptionRequete
                {
                    LigneAchatId = l.Id,
                    QuantiteRecue = l.Quantite - l.QuantiteRecue
                })
                .ToList();

            if (restantes.Count == 0)
            {
                throw new RegleMetierException(
                    "Toutes les matières de cet achat ont déjà été reçues.");
            }

            await _service.ReceptionnerAsync(
                achat.Id, new ReceptionAchatRequete { Lignes = restantes });
        },
        confirmation: "Enregistrer la réception de cet achat ?\n\n"
                      + "Les quantités restant à recevoir vont entrer en stock.",
        succes: "Les matières sont entrées en stock.");

    [RelayCommand]
    private Task AnnulerAsync() => AgirAsync(
        achat => _service.AnnulerAsync(achat.Id, "Annulé depuis l'écran des achats."),
        confirmation: "Annuler cet achat ?\n\n"
                      + "Les matières déjà reçues seront retirées du stock.",
        succes: "L'achat est annulé.");

    public override string Titre => "Achats";

    public override string Introduction => "Achats de matières premières et réceptions en stock. Un achat se saisit depuis l'écran « Nouvel achat ».";

    protected override Task<PagedResult<AchatDto>> LireAsync()
        => _service.ListerAsync(new FiltreAchatsRequete
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
        new("Fournisseur", "FournisseurNom", ColonneAlignement.Gauche),
        new("Total", "Total", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Payé", "Paye", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Reste", "Reste", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche)
    };
}
