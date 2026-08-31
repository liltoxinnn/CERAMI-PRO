using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>
/// Ordre de fabrication.
///
/// Les matières ne sont pas saisies ici : elles viennent de la recette du
/// produit, et ne sortent du stock qu'au lancement de la production.
/// </summary>
public class OrdreProductionFormulaireVueModele : FormulaireVueModele<OrdreProductionRequete>
{
    private readonly IProductionService _productions;
    private readonly IProduitService _produits;
    private readonly IRecetteService _recettes;

    public OrdreProductionFormulaireVueModele(
        IProductionService productions,
        IProduitService produits,
        IRecetteService recettes,
        IServiceLangue langue)
        : base(langue)
    {
        _productions = productions;
        _produits = produits;
        _recettes = recettes;
        _champs = Construire(Array.Empty<OptionChamp>(), Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => EstCreation ? "Nouvel ordre de production" : "Modifier l'ordre";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override async Task PreparerAsync()
    {
        var produits = await _produits.ListerAsync(
            new FiltreProduitsRequete { TaillePage = 200, InclureInactifs = false });

        var recettes = await _recettes.ListerAsync();

        _champs = Construire(
            produits.Elements.Select(p => new OptionChamp(p.Id, $"{p.Reference} — {p.Nom}")).ToList(),
            recettes.Where(r => r.Active)
                .Select(r => new OptionChamp(r.Id, $"{r.ProduitNom} — {r.Nom}"))
                .ToList());

        OnPropertyChanged(nameof(Champs));
    }

    public override async Task PreparerModificationAsync(int id)
    {
        var ordre = await _productions.ObtenirAsync(id);

        Id = id;
        Requete = new OrdreProductionRequete
        {
            ProduitId = ordre.ProduitId,
            RecetteId = ordre.RecetteId,
            CommandeId = ordre.CommandeId,
            QuantitePrevue = ordre.QuantitePrevue,
            Priorite = ordre.Priorite,
            DateDebutPrevue = ordre.DateDebutPrevue,
            DateFinPrevue = ordre.DateFinPrevue,
            EmployeId = ordre.EmployeId,
            Notes = ordre.Notes,
            CoutMainOeuvre = ordre.CoutMainOeuvre,
            CoutEmballage = ordre.CoutEmballage,
            AutresCouts = ordre.AutresCouts
        };
    }

    private static IReadOnlyList<ChampFormulaire> Construire(
        IReadOnlyList<OptionChamp> produits, IReadOnlyList<OptionChamp> recettes)
        => new ChampFormulaire[]
        {
            new("Produit à fabriquer", nameof(OrdreProductionRequete.ProduitId), TypeChamp.Liste,
                Obligatoire: true, Options: produits),
            new("Quantité prévue", nameof(OrdreProductionRequete.QuantitePrevue), TypeChamp.Nombre,
                Obligatoire: true),
            new("Recette employée", nameof(OrdreProductionRequete.RecetteId), TypeChamp.Liste,
                Options: recettes,
                Aide: "La recette indique les matières à consommer au lancement."),
            new("Priorité", nameof(OrdreProductionRequete.Priorite), TypeChamp.Liste,
                Options: Priorites),
            new("Début prévu", nameof(OrdreProductionRequete.DateDebutPrevue), TypeChamp.Date),
            new("Fin prévue", nameof(OrdreProductionRequete.DateFinPrevue), TypeChamp.Date),
            new("Main-d'œuvre", nameof(OrdreProductionRequete.CoutMainOeuvre), TypeChamp.Montant),
            new("Emballage", nameof(OrdreProductionRequete.CoutEmballage), TypeChamp.Montant),
            new("Autres coûts", nameof(OrdreProductionRequete.AutresCouts), TypeChamp.Montant),
            new("Notes", nameof(OrdreProductionRequete.Notes), TypeChamp.TexteLong)
        };

    /// <summary>Priorités proposées, avec leur nom français.</summary>
    private static IReadOnlyList<OptionChamp> Priorites { get; } =
        EnumExtensions.Libelles<Priority>()
            .Select(p => new OptionChamp((int)p.Valeur, p.Libelle))
            .ToList();

    protected override async Task EnregistrerAsync()
    {
        if (Id is { } identifiant)
        {
            await _productions.ModifierAsync(identifiant, Requete);
        }
        else
        {
            await _productions.CreerAsync(Requete);
        }
    }
}
