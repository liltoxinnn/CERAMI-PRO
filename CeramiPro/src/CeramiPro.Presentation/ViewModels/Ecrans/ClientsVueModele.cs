using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Fiches clients, historique d'achats et reste à payer.</summary>
public partial class ClientsVueModele : ListeVueModele<ClientDto>
{
    private readonly IClientService _service;

    public ClientsVueModele(IClientService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.clients"];

    public override string Introduction => "Fiches clients, historique d'achats et reste à payer.";

    protected override Task<PagedResult<ClientDto>> LireAsync()
        => _service.ListerAsync(new FiltreClientsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Nom", "Nom", ColonneAlignement.Gauche),
        new("Téléphone", "Telephone", ColonneAlignement.Gauche),
        new("Ville", "Ville", ColonneAlignement.Gauche),
        new("Total dépensé", "TotalDepenseAffiche", ColonneAlignement.Droite),
        new("Reste dû", "ResteAffiche", ColonneAlignement.Droite)
    };
}
