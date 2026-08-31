using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Travaux de décoration : peinture, émail, dorure, gravure.</summary>
public partial class DecorationsVueModele : ListeVueModele<DecorationDto>
{
    private readonly IDecorationService _service;

    public DecorationsVueModele(IDecorationService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.decoration.travaux"];

    public override string Introduction => "Travaux de décoration : peinture, émail, dorure, gravure.";

    protected override Task<PagedResult<DecorationDto>> LireAsync()
        => _service.ListerAsync(new FiltreDecorationsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Production", "ProductionNumero", ColonneAlignement.Gauche),
        new("Type", "TypeNom", ColonneAlignement.Gauche),
        new("Responsable", "ResponsableNom", ColonneAlignement.Gauche),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche)
    };
}
