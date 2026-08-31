using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Encaissements, acomptes et règlements de dettes.</summary>
public partial class PaiementsVueModele : ListeVueModele<PaiementDto>
{
    private readonly IPaiementService _service;

    public PaiementsVueModele(IPaiementService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.paiements"];

    public override string Introduction => "Encaissements, acomptes et règlements de dettes.";

    protected override Task<PagedResult<PaiementDto>> LireAsync()
        => _service.ListerAsync(new FiltrePaiementsRequete
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
        new("Montant", "MontantAffiche", ColonneAlignement.Droite),
        new("Mode", "ModeReglement", ColonneAlignement.Gauche)
    };
}
