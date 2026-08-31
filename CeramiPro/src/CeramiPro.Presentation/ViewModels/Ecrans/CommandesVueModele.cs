using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Pièces sur mesure : dimensions, couleurs, acompte et échéance.</summary>
public partial class CommandesVueModele : ListeVueModele<CommandeDto>
{
    private readonly ICommandeService _service;

    public CommandesVueModele(ICommandeService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.commandes"];

    public override string Introduction => "Pièces sur mesure : dimensions, couleurs, acompte et échéance.";

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
        new("Échéance", "EcheanceAffichee", ColonneAlignement.Gauche),
        new("Total", "TotalAffiche", ColonneAlignement.Droite),
        new("Reste", "ResteAffiche", ColonneAlignement.Droite),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche)
    };
}
