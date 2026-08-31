using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Toute entrée et toute sortie de stock, avec son motif.</summary>
public partial class MouvementsVueModele : ListeVueModele<MouvementStockDto>
{
    private readonly IInventaireService _service;

    public MouvementsVueModele(IInventaireService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.stock.mouvements"];

    public override string Introduction => "Toute entrée et toute sortie de stock, avec son motif.";

    protected override Task<PagedResult<MouvementStockDto>> LireAsync()
        => _service.ListerAsync(new FiltreMouvementsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Date", "DateAffichee", ColonneAlignement.Gauche),
        new("Type", "TypeLibelle", ColonneAlignement.Gauche),
        new("Article", "ArticleNom", ColonneAlignement.Gauche),
        new("Quantité", "QuantiteAffichee", ColonneAlignement.Droite),
        new("Avant", "QuantiteAvant", ColonneAlignement.Droite),
        new("Après", "QuantiteApres", ColonneAlignement.Droite),
        new("Référence", "Reference", ColonneAlignement.Gauche)
    };
}
