using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Common;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>
/// Saisie d'un élément d'une liste simple : catégorie de matières, de
/// produits, de dépenses, ou type de décoration.
///
/// Les quatre listes ont exactement la même fiche : une seule vue-modèle
/// suffit, le type voulu étant donné par les classes dérivées.
/// </summary>
public abstract class ReferentielFormulaireVueModele : FormulaireVueModele<ElementReferentielRequete>
{
    private readonly IReferentielService _referentiels;

    protected ReferentielFormulaireVueModele(IReferentielService referentiels, IServiceLangue langue)
        : base(langue)
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

/// <summary>Argile, émail, pigment, emballage…</summary>
public class CategorieMatiereFormulaireVueModele : ReferentielFormulaireVueModele
{
    public CategorieMatiereFormulaireVueModele(IReferentielService referentiels, IServiceLangue langue)
        : base(referentiels, langue) { }

    protected override TypeReferentiel Type => TypeReferentiel.CategorieMatiere;
}

/// <summary>Vaisselle, carrelage, décoration…</summary>
public class CategorieProduitFormulaireVueModele : ReferentielFormulaireVueModele
{
    public CategorieProduitFormulaireVueModele(IReferentielService referentiels, IServiceLangue langue)
        : base(referentiels, langue) { }

    protected override TypeReferentiel Type => TypeReferentiel.CategorieProduit;
}

/// <summary>Électricité, gaz, transport, salaires…</summary>
public class CategorieDepenseFormulaireVueModele : ReferentielFormulaireVueModele
{
    public CategorieDepenseFormulaireVueModele(IReferentielService referentiels, IServiceLangue langue)
        : base(referentiels, langue) { }

    protected override TypeReferentiel Type => TypeReferentiel.CategorieDepense;
}

/// <summary>Émaillage, peinture à la main, dorure…</summary>
public class TypeDecorationFormulaireVueModele : ReferentielFormulaireVueModele
{
    public TypeDecorationFormulaireVueModele(IReferentielService referentiels, IServiceLangue langue)
        : base(referentiels, langue) { }

    protected override TypeReferentiel Type => TypeReferentiel.TypeDecoration;
}
