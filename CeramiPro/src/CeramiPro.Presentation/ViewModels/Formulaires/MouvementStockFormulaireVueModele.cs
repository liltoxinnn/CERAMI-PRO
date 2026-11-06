using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>
/// Entrée en stock ou casse, saisie à la main : des pièces sortent du four,
/// une autre se fêle au déballage. Le stock et l'historique se mettent à jour
/// ensemble ; rien ne change sans laisser de trace.
/// </summary>
public class MouvementStockFormulaireVueModele : FormulaireVueModele<MouvementManuelRequete>
{
    private readonly IInventaireService _inventaire;
    private readonly IProduitService _produits;

    public MouvementStockFormulaireVueModele(IInventaireService inventaire, IProduitService produits)
    {
        _inventaire = inventaire;
        _produits = produits;
        _champs = Construire(Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => Requete.TypeMouvement == InventoryTransactionType.Perte
        ? "Casse ou perte"
        : "Entrée en stock";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override async Task PreparerAsync()
    {
        var produits = await _produits.ListerActifsAsync();

        _champs = Construire(produits
            .Select(p => new OptionChamp(p.Id,
                $"{p.Nom} — {p.Reference} — {Formatage.Quantite(p.StockActuel)} en stock"))
            .ToList());

        OnPropertyChanged(nameof(Champs));
    }

    /// <summary>Ouvre le formulaire sur le produit choisi dans l'écran du stock.</summary>
    public void Preparer(int produitId, InventoryTransactionType type)
    {
        Requete = new MouvementManuelRequete { ProduitId = produitId, TypeMouvement = type };
        OnPropertyChanged(nameof(Titre));
    }

    private static IReadOnlyList<ChampFormulaire> Construire(IReadOnlyList<OptionChamp> produits)
        => new ChampFormulaire[]
        {
            new("Produit", nameof(MouvementManuelRequete.ProduitId), TypeChamp.Liste,
                Obligatoire: true, Options: produits),
            new("Mouvement", nameof(MouvementManuelRequete.TypeMouvement), TypeChamp.Liste,
                Obligatoire: true,
                Options: new[]
                {
                    new OptionChamp((int)InventoryTransactionType.Entree, "Entrée en stock (pièces terminées ou reçues)"),
                    new OptionChamp((int)InventoryTransactionType.Perte, "Casse ou perte (pièces retirées du stock)")
                }),
            new("Nombre de pièces", nameof(MouvementManuelRequete.Quantite), TypeChamp.Nombre, Obligatoire: true),
            new("Coût d'une pièce", nameof(MouvementManuelRequete.CoutUnitaire), TypeChamp.Montant,
                Aide: "Pour une entrée seulement. Laissez à zéro pour garder le coût actuel ; " +
                      "sinon le coût du stock devient la moyenne des deux."),
            new("Précision", nameof(MouvementManuelRequete.Notes), TypeChamp.TexteLong,
                Aide: "Obligatoire pour une casse : fêlée à la cuisson, tombée, abîmée au transport…")
        };

    protected override Task EnregistrerAsync() => _inventaire.EnregistrerManuelAsync(Requete);
}

/// <summary>
/// Correction du stock après un comptage : on saisit ce qu'il y a vraiment
/// sur les étagères, et le logiciel enregistre l'écart, avec son motif.
/// </summary>
public class RegularisationFormulaireVueModele : FormulaireVueModele<RegularisationRequete>
{
    private readonly IInventaireService _inventaire;
    private readonly IProduitService _produits;

    public RegularisationFormulaireVueModele(IInventaireService inventaire, IProduitService produits)
    {
        _inventaire = inventaire;
        _produits = produits;
        _champs = Construire(Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => "Corriger le stock après comptage";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override async Task PreparerAsync()
    {
        var produits = await _produits.ListerActifsAsync();

        _champs = Construire(produits
            .Select(p => new OptionChamp(p.Id,
                $"{p.Nom} — {p.Reference} — {Formatage.Quantite(p.StockActuel)} selon le logiciel"))
            .ToList());

        OnPropertyChanged(nameof(Champs));
    }

    /// <summary>Propose le stock actuel : on ne corrige que ce qui diffère.</summary>
    public void Preparer(int produitId, decimal stockActuel)
        => Requete = new RegularisationRequete { ProduitId = produitId, QuantiteComptee = stockActuel };

    private static IReadOnlyList<ChampFormulaire> Construire(IReadOnlyList<OptionChamp> produits)
        => new ChampFormulaire[]
        {
            new("Produit", nameof(RegularisationRequete.ProduitId), TypeChamp.Liste,
                Obligatoire: true, Options: produits),

            // Zéro est un comptage valable : le champ n'est donc pas marqué
            // obligatoire, le service vérifie qu'il diffère du stock.
            new("Pièces comptées", nameof(RegularisationRequete.QuantiteComptee), TypeChamp.Nombre,
                Aide: "Ce qu'il y a réellement sur les étagères."),
            new("Motif", nameof(RegularisationRequete.Motif), TypeChamp.Texte, Obligatoire: true,
                Aide: "Erreur de comptage, pièce retrouvée, vente non saisie…"),
            new("Notes", nameof(RegularisationRequete.Notes), TypeChamp.TexteLong)
        };

    protected override Task EnregistrerAsync() => _inventaire.RegulariserAsync(Requete);
}
