using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Commandes passées aux fournisseurs et réceptions de matières.</summary>
public partial class AchatsVueModele : ListeVueModele<AchatDto>
{
    private readonly IAchatService _service;

    public AchatsVueModele(IAchatService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.stock.mouvements"];

    public override string Introduction => "Commandes passées aux fournisseurs et réceptions de matières.";

    protected override Task<PagedResult<AchatDto>> LireAsync()
        => _service.ListerAsync(new FiltreAchatsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Date", "DateAffichee", ColonneAlignement.Gauche),
        new("Fournisseur", "FournisseurNom", ColonneAlignement.Gauche),
        new("Total", "TotalAffiche", ColonneAlignement.Droite),
        new("Reste", "ResteAffiche", ColonneAlignement.Droite),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche)
    };
}
