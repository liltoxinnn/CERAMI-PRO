using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Fiches clients, historique d'achats et reste à payer.</summary>
public partial class ClientsVueModele : ListeVueModele<ClientDto>
{
    private readonly IClientService _service;

    public ClientsVueModele(IClientService service, IServiceLangue langue,
        IServiceFormulaire formulaires, IServiceProvider services)
        : base(langue, formulaires)
    {
        _service = service;
        _services = services;
    }

    private readonly IServiceProvider _services;

    public override bool PeutAjouter => true;

    protected override object? CreerFormulaire(int? id = null)
        => _services.GetService(typeof(ClientFormulaireVueModele));

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
