using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Catalogue des produits : fiches, prix, photos. Le stock se gère dans
/// l'écran voisin, mais se lit aussi ici.
/// </summary>
public partial class ProduitsVueModele : ListeVueModele<ProduitDto>
{
    private readonly IProduitService _service;
    private readonly FabriqueGaleries _galeries;

    public ProduitsVueModele(IProduitService service, FabriqueGaleries galeries, OutilsListe outils)
        : base(outils)
    {
        _service = service;
        _galeries = galeries;

        Filtres = new FiltreListe[]
        {
            new FiltreCase("Masquer les produits retirés de la vente", FiltreChangeAsync, actif: true)
        };
    }

    protected override Type TypeFormulaire => typeof(ProduitFormulaireVueModele);

    /// <summary>
    /// Un produit qui a déjà été vendu ne se supprime pas — le service le
    /// refuse et propose de le désactiver : l'historique des ventes doit
    /// garder sa fiche.
    /// </summary>
    public override bool PeutSupprimer => true;

    protected override Task SupprimerElementAsync(int id) => _service.SupprimerAsync(id);

    public override IReadOnlyList<FiltreListe> Filtres { get; }

    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("📷  Photos", PhotosCommand,
            Aide: "Ajouter des photos du produit et choisir celle qui le représente.")
    };

    [RelayCommand]
    private Task PhotosAsync() => OuvrirFenetreAsync(_galeries.PourProduit);

    public override string Titre => "Catalogue";

    public override string Introduction =>
        "Fiches des produits : prix, coût de revient, photos et stock. Une pièce faite en un seul " +
        "exemplaire se déclare « pièce unique ».";

    protected override Task<PagedResult<ProduitDto>> LireAsync()
        => _service.ListerAsync(new FiltreProduitsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim(),
            InclureInactifs = !((FiltreCase)Filtres[0]).Actif
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
        new("Pièce unique", "PieceUnique", ColonneAlignement.Centre),
        new("Photos", "NombrePhotos", ColonneAlignement.Droite, FormatColonne.Nombre),
        new("En vente", "Actif", ColonneAlignement.Centre)
    };
}
