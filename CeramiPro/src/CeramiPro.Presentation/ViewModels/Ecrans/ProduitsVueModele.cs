using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Catalogue des produits céramiques : prix, coût de revient et stock.</summary>
public partial class ProduitsVueModele : ListeVueModele<ProduitDto>
{
    private readonly IProduitService _service;

    public ProduitsVueModele(IProduitService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    protected override Type TypeFormulaire => typeof(ProduitFormulaireVueModele);

    public override bool PeutSupprimer => true;

    protected override Task SupprimerElementAsync(int id) => _service.SupprimerAsync(id);

    public override string Titre => Langue["menu.produits.catalogue"];

    public override string Introduction => "Catalogue des produits céramiques : prix, coût de revient et stock.";

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
        new("Prix de vente", "PrixVente", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Coût", "CoutProduction", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Marge", "TauxMarge", ColonneAlignement.Droite, FormatColonne.Pourcentage),
        new("Stock", "StockActuel", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Seuil", "StockMinimum", ColonneAlignement.Droite, FormatColonne.Quantite)
    };
}
