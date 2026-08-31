using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
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
