using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>Enregistrement d'une dépense de l'atelier.</summary>
public class DepenseFormulaireVueModele : FormulaireVueModele<DepenseRequete>
{
    private readonly IDepenseService _depenses;
    private readonly IReferentielService _referentiels;

    public DepenseFormulaireVueModele(
        IDepenseService depenses, IReferentielService referentiels, IServiceLangue langue)
        : base(langue)
    {
        _depenses = depenses;
        _referentiels = referentiels;
        _champs = ConstruireChamps(Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => EstCreation ? "Nouvelle dépense" : "Modifier la dépense";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    /// <summary>Charge les catégories avant d'afficher le formulaire.</summary>
    public async Task PreparerAsync()
    {
        var categories = await _referentiels.ListerAsync(TypeReferentiel.CategorieDepense);

        _champs = ConstruireChamps(categories
            .Select(c => new OptionChamp(c.Id, c.Nom))
            .ToList());

        OnPropertyChanged(nameof(Champs));
    }

    private static IReadOnlyList<ChampFormulaire> ConstruireChamps(IReadOnlyList<OptionChamp> categories)
        => new ChampFormulaire[]
        {
            new("Catégorie", nameof(DepenseRequete.CategorieId), TypeChamp.Liste,
                Obligatoire: true, Options: categories),
            new("Montant", nameof(DepenseRequete.Montant), TypeChamp.Montant, Obligatoire: true),
            new("Date", nameof(DepenseRequete.Date), TypeChamp.Date),
            new("Description", nameof(DepenseRequete.Description), TypeChamp.TexteLong,
                Obligatoire: true),
            new("Justificatif", nameof(DepenseRequete.Justificatif), TypeChamp.Texte,
                Aide: "Numéro ou référence de la pièce justificative")
        };

    protected override async Task EnregistrerAsync()
    {
        if (Id is { } identifiant)
        {
            await _depenses.ModifierAsync(identifiant, Requete);
        }
        else
        {
            await _depenses.CreerAsync(Requete);
        }
    }
}
