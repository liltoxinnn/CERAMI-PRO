using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Enums;
using CommunityToolkit.Mvvm.Input;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Commandes personnalisées : pièces sur mesure demandées par un client.</summary>
public partial class CommandesVueModele : ListeVueModele<CommandeDto>
{
    private readonly ICommandeService _service;

    public CommandesVueModele(ICommandeService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    protected override Type TypeFormulaire => typeof(CommandeFormulaireVueModele);

    /// <summary>
    /// Une commande personnalisée avance de l'acceptation à la livraison.
    /// L'étape suivante se déduit de l'étape courante : il n'y a donc qu'un
    /// bouton, dont le libellé change.
    /// </summary>
    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("Faire avancer la commande", AvancerCommand,
            Aide: "Passe la commande à l'étape suivante de son parcours."),
        new("Marquer comme livrée", LivrerCommand),
        new("Annuler la commande", AnnulerCommand, Destructive: true)
    };

    [RelayCommand]
    private Task AvancerAsync() => AgirAsync(
        async commande =>
        {
            if (EtapeApres(commande.Statut) is not { } suivante)
            {
                throw new RegleMetierException(
                    $"La commande {commande.Numero} est « {commande.StatutLibelle} » : "
                    + "elle n'a plus d'étape suivante.");
            }

            await _service.ChangerStatutAsync(commande.Id, suivante);
        },
        succes: "La commande a été mise à jour.");

    [RelayCommand]
    private Task LivrerAsync() => AgirAsync(
        commande => _service.ChangerStatutAsync(commande.Id, CustomOrderStatus.Livre),
        confirmation: "Marquer cette commande comme livrée au client ?",
        succes: "La commande est livrée.");

    [RelayCommand]
    private Task AnnulerAsync() => AgirAsync(
        commande => _service.AnnulerAsync(commande.Id, "Annulée depuis l'écran des commandes."),
        confirmation: "Annuler cette commande ?",
        succes: "La commande est annulée.");

    /// <summary>
    /// Étape qui suit dans le parcours d'une commande. L'annulation n'en
    /// fait pas partie : elle a son propre bouton.
    /// </summary>
    private static CustomOrderStatus? EtapeApres(CustomOrderStatus etape)
        => Enum.GetValues<CustomOrderStatus>()
            .Where(e => e != CustomOrderStatus.Annule)
            .OrderBy(e => (int)e)
            .SkipWhile(e => e != etape)
            .Skip(1)
            .Cast<CustomOrderStatus?>()
            .FirstOrDefault();

    public override string Titre => Langue["menu.commandes"];

    public override string Introduction => "Commandes personnalisées : pièces sur mesure demandées par un client.";

    protected override Task<PagedResult<CommandeDto>> LireAsync()
        => _service.ListerAsync(new FiltreCommandesRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Client", "ClientNom", ColonneAlignement.Gauche),
        new("Titre", "Titre", ColonneAlignement.Gauche),
        new("Quantité", "Quantite", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Échéance", "DateLimite", ColonneAlignement.Gauche, FormatColonne.Date),
        new("Total", "Total", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Reste", "Reste", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche),
        new("En retard", "EnRetard", ColonneAlignement.Centre)
    };
}
