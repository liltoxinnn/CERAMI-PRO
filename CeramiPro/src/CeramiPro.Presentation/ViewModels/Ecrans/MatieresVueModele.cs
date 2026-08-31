using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Argile, émail, pigments, emballage : stock, seuil d'alerte et coût moyen.</summary>
public partial class MatieresVueModele : ListeVueModele<MatiereDto>
{
    private readonly IMatiereService _service;

    public MatieresVueModele(IMatiereService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.stock.matieres"];

    public override string Introduction => "Argile, émail, pigments, emballage : stock, seuil d'alerte et coût moyen.";

    protected override Task<PagedResult<MatiereDto>> LireAsync()
        => _service.ListerAsync(new FiltreMatieresRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Référence", "Reference", ColonneAlignement.Gauche),
        new("Nom", "Nom", ColonneAlignement.Gauche),
        new("Catégorie", "CategorieNom", ColonneAlignement.Gauche),
        new("Stock", "StockAffiche", ColonneAlignement.Droite),
        new("Seuil", "StockMinimum", ColonneAlignement.Droite),
        new("Coût moyen", "CoutMoyenAffiche", ColonneAlignement.Droite)
    };
}
