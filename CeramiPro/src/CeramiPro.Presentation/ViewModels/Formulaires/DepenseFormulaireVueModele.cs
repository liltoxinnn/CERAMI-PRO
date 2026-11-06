using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.DTOs.Referentiels;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>Enregistrement d'une dépense de l'atelier.</summary>
public class DepenseFormulaireVueModele : FormulaireVueModele<DepenseRequete>
{
    private readonly IDepenseService _depenses;
    private readonly IReferentielService _referentiels;

    public DepenseFormulaireVueModele(IDepenseService depenses, IReferentielService referentiels)
    {
        _depenses = depenses;
        _referentiels = referentiels;
        _champs = ConstruireChamps(Array.Empty<OptionChamp>(), Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => EstCreation ? "Nouvelle dépense" : "Modifier la dépense";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    /// <summary>Charge les catégories avant d'afficher le formulaire.</summary>
    public override async Task PreparerAsync()
    {
        var categories = await _referentiels.ListerAsync(TypeReferentiel.CategorieDepense, false);
        var modes = await _referentiels.ListerModesReglementAsync();

        _champs = ConstruireChamps(
            categories.Select(c => new OptionChamp(c.Id, c.Nom)).ToList(),
            modes.Where(m => m.Actif).Select(m => new OptionChamp(m.Id, m.Nom)).ToList());

        OnPropertyChanged(nameof(Champs));
    }

    /// <summary>Reprend la dépense choisie : on corrige, on ne ressaisit pas.</summary>
    public override async Task PreparerModificationAsync(int id)
    {
        var depense = await _depenses.ObtenirAsync(id);

        Id = id;
        Requete = new DepenseRequete
        {
            CategorieId = depense.CategorieId,
            Montant = depense.Montant,
            Date = depense.Date.Date,
            Description = depense.Description,
            Justificatif = depense.Justificatif,
            ModeReglementId = depense.ModeReglementId
        };
    }

    private static IReadOnlyList<ChampFormulaire> ConstruireChamps(
        IReadOnlyList<OptionChamp> categories, IReadOnlyList<OptionChamp> modes)
        => new ChampFormulaire[]
        {
            new("Catégorie", nameof(DepenseRequete.CategorieId), TypeChamp.Liste,
                Obligatoire: true, Options: categories,
                Aide: "Les catégories se gèrent dans « Paramètres › Catégories de dépenses »."),
            new("Montant", nameof(DepenseRequete.Montant), TypeChamp.Montant, Obligatoire: true),
            new("Date", nameof(DepenseRequete.Date), TypeChamp.Date,
                Aide: "Laissez vide pour aujourd'hui."),
            new("Description", nameof(DepenseRequete.Description), TypeChamp.TexteLong,
                Obligatoire: true, Aide: "Par exemple : « Argile rouge, 50 kg » ou « Facture d'électricité d'octobre »."),
            new("Payée par", nameof(DepenseRequete.ModeReglementId), TypeChamp.Liste, Options: modes),
            new("Justificatif", nameof(DepenseRequete.Justificatif), TypeChamp.Texte,
                Aide: "Numéro ou référence de la facture ou du ticket.")
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
