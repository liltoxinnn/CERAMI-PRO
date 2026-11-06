using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>
/// Saisie d'une catégorie, de produits ou de dépenses.
///
/// Les quatre listes ont exactement la même fiche : une seule vue-modèle
/// suffit, le type voulu étant donné par les classes dérivées.
/// </summary>
public abstract class ReferentielFormulaireVueModele : FormulaireVueModele<ElementReferentielRequete>
{
    private readonly IReferentielService _referentiels;

    protected ReferentielFormulaireVueModele(IReferentielService referentiels)
        : base()
        => _referentiels = referentiels;

    /// <summary>Liste concernée.</summary>
    protected abstract TypeReferentiel Type { get; }

    public override string Titre => EstCreation
        ? "Nouvel élément — " + Type.Libelle()
        : "Modifier — " + Type.Libelle();

    public override IReadOnlyList<ChampFormulaire> Champs { get; } = new ChampFormulaire[]
    {
        new("Nom", nameof(ElementReferentielRequete.Nom), TypeChamp.Texte, Obligatoire: true),
        new("Description", nameof(ElementReferentielRequete.Description), TypeChamp.TexteLong),
        new("Actif", nameof(ElementReferentielRequete.Actif), TypeChamp.Case)
    };

    public override async Task PreparerModificationAsync(int id)
    {
        var elements = await _referentiels.ListerAsync(Type);

        if (elements.FirstOrDefault(e => e.Id == id) is not { } element)
        {
            return;
        }

        Id = id;
        Requete = new ElementReferentielRequete
        {
            Nom = element.Nom,
            Description = element.Description,
            Actif = element.Actif
        };
    }

    protected override async Task EnregistrerAsync()
    {
        if (Id is { } identifiant)
        {
            await _referentiels.ModifierAsync(Type, identifiant, Requete);
        }
        else
        {
            await _referentiels.CreerAsync(Type, Requete);
        }
    }
}

/// <summary>Vases, assiettes, statues, objets décoratifs…</summary>
public class CategorieProduitFormulaireVueModele : ReferentielFormulaireVueModele
{
    public CategorieProduitFormulaireVueModele(IReferentielService referentiels)
        : base(referentiels) { }

    protected override TypeReferentiel Type => TypeReferentiel.CategorieProduit;
}

/// <summary>Électricité, gaz, transport, salaires…</summary>
public class CategorieDepenseFormulaireVueModele : ReferentielFormulaireVueModele
{
    public CategorieDepenseFormulaireVueModele(IReferentielService referentiels)
        : base(referentiels) { }

    protected override TypeReferentiel Type => TypeReferentiel.CategorieDepense;
}
