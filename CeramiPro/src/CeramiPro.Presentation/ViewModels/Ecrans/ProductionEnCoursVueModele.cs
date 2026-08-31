using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Ordres de fabrication encore ouverts. C'est l'écran que l'on garde
/// affiché dans l'atelier : ce qui reste à faire, et ce qui est en retard.
/// </summary>
public class ProductionEnCoursVueModele : ListeVueModele<OrdreProductionDto>
{
    private readonly IProductionService _service;

    public ProductionEnCoursVueModele(
        IProductionService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    public override string Titre => Langue["menu.production.enCours"];

    public override string Introduction =>
        "Ordres de fabrication encore ouverts, de la préparation à l'emballage.";

    protected override Task<PagedResult<OrdreProductionDto>> LireAsync()
        => _service.ListerAsync(new FiltreProductionsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim(),
            SeulementEnCours = true
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero"),
        new("Produit", "ProduitNom"),
        new("Prévu", "QuantitePrevue", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Terminé", "QuantiteTerminee", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Étape", "StatutLibelle"),
        new("Priorité", "PrioriteLibelle"),
        new("Responsable", "EmployeNom"),
        new("Fin prévue", "DateFinPrevue", ColonneAlignement.Gauche, FormatColonne.Date),
        new("En retard", "EnRetard", ColonneAlignement.Centre)
    };
}
