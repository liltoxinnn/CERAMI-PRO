using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Écran d'une liste simple gérée par l'atelier lui-même.
///
/// Les catégories de produits et celles de dépenses ont le même écran :
/// seul le type change.
/// </summary>
public abstract class ReferentielVueModele : ListeSimpleVueModele<ElementReferentielDto>
{
    private readonly IReferentielService _service;

    protected ReferentielVueModele(IReferentielService service, OutilsListe outils)
        : base(outils)
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

/// <summary>Vases, assiettes, statues, objets décoratifs…</summary>
public class CategoriesProduitsVueModele : ReferentielVueModele
{
    public CategoriesProduitsVueModele(
        IReferentielService service, OutilsListe outils)
        : base(service, outils) { }

    protected override TypeReferentiel Type => TypeReferentiel.CategorieProduit;

    protected override Type TypeFormulaire => typeof(CategorieProduitFormulaireVueModele);

    public override string Titre => "Catégories";

    public override string Introduction =>
        "Familles de produits du catalogue. Une catégorie employée par un produit ne peut plus être supprimée.";
}

/// <summary>Électricité, gaz, transport, salaires…</summary>
public class CategoriesDepensesVueModele : ReferentielVueModele
{
    public CategoriesDepensesVueModele(
        IReferentielService service, OutilsListe outils)
        : base(service, outils) { }

    protected override TypeReferentiel Type => TypeReferentiel.CategorieDepense;

    protected override Type TypeFormulaire => typeof(CategorieDepenseFormulaireVueModele);

    public override string Titre => "Catégories de dépenses";

    public override string Introduction =>
        "Postes de dépense de l'atelier. Ce sont eux qui structurent le rapport des dépenses.";
}
