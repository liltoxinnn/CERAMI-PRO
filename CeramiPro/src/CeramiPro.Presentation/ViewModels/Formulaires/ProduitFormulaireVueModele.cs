using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Application.Interfaces;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>Création et modification d'un produit du catalogue.</summary>
public class ProduitFormulaireVueModele : FormulaireVueModele<ProduitRequete>
{
    private readonly IProduitService _produits;
    private readonly IReferentielService _referentiels;

    public ProduitFormulaireVueModele(
        IProduitService produits, IReferentielService referentiels)
        : base()
    {
        _produits = produits;
        _referentiels = referentiels;
        _champs = Construire(Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => EstCreation ? "Nouveau produit" : "Modifier le produit";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override async Task PreparerAsync()
    {
        var categories = await _referentiels.ListerAsync(TypeReferentiel.CategorieProduit, false);

        _champs = Construire(categories.Select(c => new OptionChamp(c.Id, c.Nom)).ToList());
        OnPropertyChanged(nameof(Champs));
    }

    public override async Task PreparerModificationAsync(int id)
    {
        var produit = await _produits.ObtenirAsync(id);

        Id = id;
        Requete = new ProduitRequete
        {
            Nom = produit.Nom,
            CategorieId = produit.CategorieId,
            Description = produit.Description,
            Matiere = produit.Matiere,
            Couleur = produit.Couleur,
            Finition = produit.Finition,
            Largeur = produit.Largeur,
            Hauteur = produit.Hauteur,
            Profondeur = produit.Profondeur,
            Poids = produit.Poids,
            CoutProduction = produit.CoutProduction,
            PrixVente = produit.PrixVente,
            StockMinimum = produit.StockMinimum,
            Personnalisable = produit.Personnalisable,
            PieceUnique = produit.PieceUnique,
            Actif = produit.Actif
        };

        _champs = _champs.Where(c => c.Propriete != nameof(ProduitRequete.StockInitial)).ToList();
        OnPropertyChanged(nameof(Champs));
    }

    private static IReadOnlyList<ChampFormulaire> Construire(IReadOnlyList<OptionChamp> categories)
        => new ChampFormulaire[]
        {
            new("Nom", nameof(ProduitRequete.Nom), TypeChamp.Texte, Obligatoire: true),
            new("Catégorie", nameof(ProduitRequete.CategorieId), TypeChamp.Liste,
                Obligatoire: true, Options: categories),
            new("Prix de vente", nameof(ProduitRequete.PrixVente), TypeChamp.Montant, Obligatoire: true),
            new("Coût de production", nameof(ProduitRequete.CoutProduction), TypeChamp.Montant,
                Aide: "Ce que coûte la pièce à fabriquer : il sert au calcul des marges et des bénéfices."),
            new("Pièce unique", nameof(ProduitRequete.PieceUnique), TypeChamp.Case,
                Aide: "Faite à la main en un seul exemplaire : son stock vaut 0 ou 1, " +
                      "et une fois vendue elle ne déclenche aucune alerte."),
            new("Stock initial", nameof(ProduitRequete.StockInitial), TypeChamp.Nombre,
                Aide: "Pièces déjà fabriquées et disponibles à la vente. Ensuite, le stock se met à jour " +
                      "tout seul à chaque vente, et par les entrées saisies dans « Stock »."),
            new("Seuil d'alerte", nameof(ProduitRequete.StockMinimum), TypeChamp.Nombre,
                Aide: "En dessous, le produit apparaît dans « Stock faible » sur le tableau de bord."),
            new("Matière", nameof(ProduitRequete.Matiere), TypeChamp.Texte,
                Aide: "Grès, faïence, porcelaine…"),
            new("Couleur", nameof(ProduitRequete.Couleur)),
            new("Finition", nameof(ProduitRequete.Finition), TypeChamp.Texte,
                Aide: "Émaillé, mat, brillant, brut…"),
            new("Largeur (cm)", nameof(ProduitRequete.Largeur), TypeChamp.Nombre),
            new("Hauteur (cm)", nameof(ProduitRequete.Hauteur), TypeChamp.Nombre),
            new("Profondeur (cm)", nameof(ProduitRequete.Profondeur), TypeChamp.Nombre),
            new("Poids (kg)", nameof(ProduitRequete.Poids), TypeChamp.Nombre),
            new("Description", nameof(ProduitRequete.Description), TypeChamp.TexteLong),
            new("Personnalisable sur commande", nameof(ProduitRequete.Personnalisable), TypeChamp.Case),
            new("Produit actif", nameof(ProduitRequete.Actif), TypeChamp.Case)
        };

    protected override async Task EnregistrerAsync()
    {
        if (Id is { } identifiant)
        {
            await _produits.ModifierAsync(identifiant, Requete);
        }
        else
        {
            await _produits.CreerAsync(Requete);
        }
    }
}
