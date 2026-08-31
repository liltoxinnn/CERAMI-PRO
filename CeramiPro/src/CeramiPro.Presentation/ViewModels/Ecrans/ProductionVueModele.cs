using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Ordres de fabrication, du façonnage à l'emballage.</summary>
public partial class ProductionVueModele : ListeVueModele<OrdreProductionDto>
{
    private readonly IProductionService _service;

    public ProductionVueModele(IProductionService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    protected override Type TypeFormulaire => typeof(OrdreProductionFormulaireVueModele);

    public override string Titre => Langue["menu.production.ordres"];

    public override string Introduction => "Ordres de fabrication, du façonnage à l'emballage.";

    protected override Task<PagedResult<OrdreProductionDto>> LireAsync()
        => _service.ListerAsync(new FiltreProductionsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Produit", "ProduitNom", ColonneAlignement.Gauche),
        new("Prévu", "QuantitePrevue", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Terminé", "QuantiteTerminee", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Cassé", "QuantiteEndommagee", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Étape", "StatutLibelle", ColonneAlignement.Gauche),
        new("Priorité", "PrioriteLibelle", ColonneAlignement.Gauche),
        new("Fin prévue", "DateFinPrevue", ColonneAlignement.Gauche, FormatColonne.Date),
        new("En retard", "EnRetard", ColonneAlignement.Centre)
    };
}
