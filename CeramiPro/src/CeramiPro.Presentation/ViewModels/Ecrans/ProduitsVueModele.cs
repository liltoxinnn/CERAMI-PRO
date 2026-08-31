using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Vases, assiettes, sculptures : prix, coût de production et stock.</summary>
public partial class ProduitsVueModele : ListeVueModele<ProduitDto>
{
    private readonly IProduitService _service;

    public ProduitsVueModele(IProduitService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.produits.catalogue"];

    public override string Introduction => "Vases, assiettes, sculptures : prix, coût de production et stock.";

    protected override Task<PagedResult<ProduitDto>> LireAsync()
        => _service.ListerAsync(new FiltreProduitsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Référence", "Reference", ColonneAlignement.Gauche),
        new("Nom", "Nom", ColonneAlignement.Gauche),
        new("Catégorie", "CategorieNom", ColonneAlignement.Gauche),
        new("Prix", "PrixVenteAffiche", ColonneAlignement.Droite),
        new("Coût", "CoutProductionAffiche", ColonneAlignement.Droite),
        new("Stock", "StockActuel", ColonneAlignement.Droite)
    };
}
