using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Fabrications terminées, avec leur coût de revient réel : c'est ce qui
/// permet de comparer le coût estimé et le coût constaté.
/// </summary>
public class HistoriqueProductionVueModele : ListeVueModele<OrdreProductionDto>
{
    private readonly IProductionService _service;

    public HistoriqueProductionVueModele(
        IProductionService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    public override string Titre => Langue["menu.production.historique"];

    public override string Introduction =>
        "Fabrications terminées, avec les quantités obtenues et le coût de revient réellement constaté.";

    protected override Task<PagedResult<OrdreProductionDto>> LireAsync()
        => _service.ListerAsync(new FiltreProductionsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim(),
            Statut = ProductionStatus.Termine
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero"),
        new("Produit", "ProduitNom"),
        new("Terminé le", "DateFinReelle", ColonneAlignement.Gauche, FormatColonne.Date),
        new("Prévu", "QuantitePrevue", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Obtenu", "QuantiteTerminee", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Cassé", "QuantiteEndommagee", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Coût matières", "CoutMatieresReel", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Coût total", "CoutTotal", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Coût unitaire", "CoutUnitaire", ColonneAlignement.Droite, FormatColonne.Montant)
    };
}
