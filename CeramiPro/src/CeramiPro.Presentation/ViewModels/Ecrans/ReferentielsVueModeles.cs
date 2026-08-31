using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Écran d'une liste simple gérée par l'atelier lui-même.
///
/// Les quatre listes — catégories de matières, de produits, de dépenses et
/// types de décoration — ont le même écran : seul le type change.
/// </summary>
public abstract class ReferentielVueModele : ListeSimpleVueModele<ElementReferentielDto>
{
    private readonly IReferentielService _service;

    protected ReferentielVueModele(IReferentielService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    protected abstract TypeReferentiel Type { get; }

    public override bool PeutSupprimer => true;

    protected override Task<IReadOnlyList<ElementReferentielDto>> LireToutesAsync()
        => _service.ListerAsync(Type);

    protected override Task SupprimerElementAsync(int id) => _service.SupprimerAsync(Type, id);

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Nom", "Nom"),
        new("Description", "Description"),
        new("Utilisations", "NombreUtilisations", ColonneAlignement.Droite, FormatColonne.Nombre),
        new("Fournie avec le logiciel", "Systeme", ColonneAlignement.Centre),
        new("Active", "Actif", ColonneAlignement.Centre)
    };
}

/// <summary>Argile, émail, pigment, emballage…</summary>
public class CategoriesMatieresVueModele : ReferentielVueModele
{
    public CategoriesMatieresVueModele(
        IReferentielService service, IServiceLangue langue, OutilsListe outils)
        : base(service, langue, outils) { }

    protected override TypeReferentiel Type => TypeReferentiel.CategorieMatiere;

    protected override Type TypeFormulaire => typeof(CategorieMatiereFormulaireVueModele);

    public override string Titre => "Catégories de matières";

    public override string Introduction =>
        "Familles de matières premières. Une catégorie employée par une matière ne peut plus être supprimée.";
}

/// <summary>Vaisselle, carrelage, décoration…</summary>
public class CategoriesProduitsVueModele : ReferentielVueModele
{
    public CategoriesProduitsVueModele(
        IReferentielService service, IServiceLangue langue, OutilsListe outils)
        : base(service, langue, outils) { }

    protected override TypeReferentiel Type => TypeReferentiel.CategorieProduit;

    protected override Type TypeFormulaire => typeof(CategorieProduitFormulaireVueModele);

    public override string Titre => Langue["menu.produits.categories"];

    public override string Introduction =>
        "Familles de produits du catalogue. Une catégorie employée par un produit ne peut plus être supprimée.";
}

/// <summary>Électricité, gaz, transport, salaires…</summary>
public class CategoriesDepensesVueModele : ReferentielVueModele
{
    public CategoriesDepensesVueModele(
        IReferentielService service, IServiceLangue langue, OutilsListe outils)
        : base(service, langue, outils) { }

    protected override TypeReferentiel Type => TypeReferentiel.CategorieDepense;

    protected override Type TypeFormulaire => typeof(CategorieDepenseFormulaireVueModele);

    public override string Titre => Langue["menu.administration.categoriesDepenses"];

    public override string Introduction =>
        "Postes de dépense de l'atelier. Ce sont eux qui structurent le rapport des dépenses.";
}

/// <summary>Émaillage, peinture à la main, dorure…</summary>
public class TypesDecorationVueModele : ReferentielVueModele
{
    public TypesDecorationVueModele(
        IReferentielService service, IServiceLangue langue, OutilsListe outils)
        : base(service, langue, outils) { }

    protected override TypeReferentiel Type => TypeReferentiel.TypeDecoration;

    protected override Type TypeFormulaire => typeof(TypeDecorationFormulaireVueModele);

    public override string Titre => Langue["menu.decoration.types"];

    public override string Introduction =>
        "Techniques de décoration pratiquées dans l'atelier : émaillage, peinture à la main, dorure…";
}
