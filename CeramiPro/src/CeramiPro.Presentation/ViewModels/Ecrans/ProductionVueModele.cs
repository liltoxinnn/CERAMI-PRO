using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Ordres de fabrication et suivi des étapes.</summary>
public partial class ProductionVueModele : ListeVueModele<OrdreProductionDto>
{
    private readonly IProductionService _service;

    public ProductionVueModele(IProductionService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.production.ordres"];

    public override string Introduction => "Ordres de fabrication et suivi des étapes.";

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
        new("Prévu", "QuantitePrevue", ColonneAlignement.Droite),
        new("Produit", "QuantiteProduite", ColonneAlignement.Droite),
        new("Étape", "StatutLibelle", ColonneAlignement.Gauche),
        new("Échéance", "EcheanceAffichee", ColonneAlignement.Gauche)
    };
}
