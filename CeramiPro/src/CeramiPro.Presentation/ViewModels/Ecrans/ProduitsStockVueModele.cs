using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Stock des produits finis.
///
/// C'est le même catalogue que l'écran « Produits », mais présenté du point
/// de vue du magasin : quantités, seuils et valeur immobilisée, plutôt que
/// prix de vente et description.
/// </summary>
public partial class ProduitsStockVueModele : ListeVueModele<ProduitDto>
{
    private readonly IProduitService _service;

    public ProduitsStockVueModele(IProduitService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    public override string Titre => Langue["menu.stock.produitsFinis"];

    public override string Introduction =>
        "Pièces terminées et disponibles à la vente. Cochez « seulement le stock faible » " +
        "pour ne voir que ce qu'il faut refabriquer.";

    /// <summary>Ne montre que les produits passés sous leur seuil d'alerte.</summary>
    [ObservableProperty]
    private bool _seulementStockFaible;

    partial void OnSeulementStockFaibleChanged(bool value) => _ = RafraichirAsync();

    protected override Task<PagedResult<ProduitDto>> LireAsync()
        => _service.ListerAsync(new FiltreProduitsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim(),
            SeulementStockFaible = SeulementStockFaible,
            InclureInactifs = false
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Référence", "Reference"),
        new("Nom", "Nom"),
        new("Catégorie", "CategorieNom"),
        new("En stock", "StockActuel", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Seuil", "StockMinimum", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Sous le seuil", "StockFaible", ColonneAlignement.Centre),
        new("Prix de vente", "PrixVente", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Coût", "CoutProduction", ColonneAlignement.Droite, FormatColonne.Montant)
    };
}
