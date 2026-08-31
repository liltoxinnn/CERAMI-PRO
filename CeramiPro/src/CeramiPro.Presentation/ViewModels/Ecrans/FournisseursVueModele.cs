using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Fiches fournisseurs, achats et règlements.</summary>
public partial class FournisseursVueModele : ListeVueModele<FournisseurDto>
{
    private readonly IFournisseurService _service;

    public FournisseursVueModele(IFournisseurService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.fournisseurs"];

    public override string Introduction => "Fiches fournisseurs, achats et règlements.";

    protected override Task<PagedResult<FournisseurDto>> LireAsync()
        => _service.ListerAsync(new FiltreFournisseursRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Nom", "Nom", ColonneAlignement.Gauche),
        new("Téléphone", "Telephone", ColonneAlignement.Gauche),
        new("Ville", "Ville", ColonneAlignement.Gauche),
        new("Reste dû", "ResteAffiche", ColonneAlignement.Droite)
    };
}
