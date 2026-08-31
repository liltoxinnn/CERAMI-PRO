using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Factures émises, payées et restant dues.</summary>
public partial class FacturesVueModele : ListeVueModele<FactureDto>
{
    private readonly IFactureService _service;

    public FacturesVueModele(IFactureService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.factures"];

    public override string Introduction => "Factures émises, payées et restant dues.";

    protected override Task<PagedResult<FactureDto>> LireAsync()
        => _service.ListerAsync(new FiltreFacturesRequete
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
        new("Client", "ClientNom", ColonneAlignement.Gauche),
        new("Total", "TotalAffiche", ColonneAlignement.Droite),
        new("Reste", "ResteAffiche", ColonneAlignement.Droite),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche)
    };
}
