using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Stock des produits : ce qui est sur les étagères, et les trois gestes qui
/// le modifient à la main — une entrée, une casse, une correction après
/// comptage. Les ventes, elles, le mettent à jour toutes seules.
/// </summary>
public partial class ProduitsStockVueModele : ListeVueModele<ProduitDto>
{
    private readonly IProduitService _service;

    public ProduitsStockVueModele(IProduitService service, OutilsListe outils)
        : base(outils)
    {
        _service = service;

        Filtres = new FiltreListe[]
        {
            new FiltreCase("Seulement le stock faible (à refabriquer)", FiltreChangeAsync)
        };
    }

    public override string Titre => "Stock";

    public override string Introduction =>
        "Quantités disponibles. Les ventes retirent les pièces vendues toutes seules ; " +
        "enregistrez ici ce qui entre, ce qui casse, et les écarts constatés en comptant.";

    public override IReadOnlyList<FiltreListe> Filtres { get; }

    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("➕  Entrée en stock", EntreeCommand,
            Aide: "Pièces terminées ou reçues : le stock augmente."),
        new("Casse ou perte", CasseCommand,
            Aide: "Pièces cassées, fêlées ou perdues : le stock diminue, avec le motif."),
        new("Corriger après comptage", CorrigerCommand,
            Aide: "Indiquez ce qu'il y a vraiment sur les étagères : l'écart est enregistré.")
    };

    [RelayCommand]
    private Task EntreeAsync() => MouvementAsync(InventoryTransactionType.Entree);

    [RelayCommand]
    private Task CasseAsync() => MouvementAsync(InventoryTransactionType.Perte);

    private Task MouvementAsync(InventoryTransactionType type)
    {
        var produit = ElementSelectionne;

        return OuvrirFormulaireAsync<MouvementStockFormulaireVueModele>(formulaire =>
        {
            // Sans ligne choisie, le formulaire s'ouvre et l'on choisit le produit dedans.
            formulaire.Preparer(produit?.Id ?? 0, type);
            return Task.CompletedTask;
        });
    }

    [RelayCommand]
    private Task CorrigerAsync()
    {
        var produit = ElementSelectionne;

        return OuvrirFormulaireAsync<RegularisationFormulaireVueModele>(formulaire =>
        {
            formulaire.Preparer(produit?.Id ?? 0, produit?.StockActuel ?? 0m);
            return Task.CompletedTask;
        });
    }

    protected override Task<PagedResult<ProduitDto>> LireAsync()
        => _service.ListerAsync(new FiltreProduitsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim(),
            SeulementStockFaible = ((FiltreCase)Filtres[0]).Actif,
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
        new("À refabriquer", "StockFaible", ColonneAlignement.Centre),
        new("Pièce unique", "PieceUnique", ColonneAlignement.Centre),
        new("Coût d'une pièce", "CoutProduction", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Prix de vente", "PrixVente", ColonneAlignement.Droite, FormatColonne.Montant)
    };
}
