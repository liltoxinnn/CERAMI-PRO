using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Recettes de fabrication : les matières nécessaires pour produire une
/// quantité donnée d'un produit, et le coût de revient qui en découle.
/// </summary>
public class RecettesVueModele : ListeSimpleVueModele<RecetteDto>
{
    private readonly IRecetteService _service;

    public RecettesVueModele(IRecetteService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    public override bool PeutSupprimer => true;

    public override string Titre => Langue["menu.produits.recettes"];

    public override string Introduction =>
        "Matières nécessaires à la fabrication d'un produit, et coût de revient calculé. " +
        "Une recette est employée par les ordres de production pour sortir les matières du stock.";

    protected override Task<IReadOnlyList<RecetteDto>> LireToutesAsync() => _service.ListerAsync();

    protected override Task SupprimerElementAsync(int id) => _service.SupprimerAsync(id);

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Produit", "ProduitNom"),
        new("Recette", "Nom"),
        new("Version", "Version", ColonneAlignement.Centre, FormatColonne.Nombre),
        new("Rendement", "Rendement", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Coût matières", "CoutMatieres", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Coût total", "CoutTotal", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Coût unitaire", "CoutUnitaire", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Par défaut", "ParDefaut", ColonneAlignement.Centre),
        new("Active", "Active", ColonneAlignement.Centre)
    };
}
