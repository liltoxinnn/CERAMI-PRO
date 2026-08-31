using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Lots de cuisson : four, température, durée et coût d'énergie.</summary>
public partial class CuissonsVueModele : ListeVueModele<CuissonDto>
{
    private readonly ICuissonService _service;

    public CuissonsVueModele(ICuissonService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.cuisson.lots"];

    public override string Introduction => "Lots de cuisson : four, température, durée et coût d'énergie.";

    protected override Task<PagedResult<CuissonDto>> LireAsync()
        => _service.ListerAsync(new FiltreCuissonsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Four", "FourNom", ColonneAlignement.Gauche),
        new("Température", "TemperatureAffichee", ColonneAlignement.Droite),
        new("Début", "DebutAffiche", ColonneAlignement.Gauche),
        new("Durée", "DureeAffichee", ColonneAlignement.Gauche),
        new("Coût", "CoutAffiche", ColonneAlignement.Droite)
    };
}
