using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Ventes enregistrées au comptoir.</summary>
public partial class VentesVueModele : ListeVueModele<VenteDto>
{
    private readonly IVenteService _service;

    public VentesVueModele(IVenteService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.ventes"];

    public override string Introduction => "Ventes enregistrées au comptoir.";

    protected override Task<PagedResult<VenteDto>> LireAsync()
        => _service.ListerAsync(new FiltreVentesRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Date", "DateAffichee", ColonneAlignement.Gauche),
        new("Client", "ClientNom", ColonneAlignement.Gauche),
        new("Total", "TotalAffiche", ColonneAlignement.Droite),
        new("Payé", "PayeAffiche", ColonneAlignement.Droite),
        new("Reste", "ResteAffiche", ColonneAlignement.Droite)
    };
}
