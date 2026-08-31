using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Identity;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Comptes, rôles et droits d'accès.</summary>
public partial class UtilisateursVueModele : ListeVueModele<UtilisateurDto>
{
    private readonly IUtilisateurService _service;

    public UtilisateursVueModele(IUtilisateurService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["commun.utilisateur"];

    public override string Introduction => "Comptes, rôles et droits d'accès.";

    protected override Task<PagedResult<UtilisateurDto>> LireAsync()
        => _service.ListerAsync(new PagedRequest
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Nom d'utilisateur", "NomUtilisateur", ColonneAlignement.Gauche),
        new("Nom complet", "NomComplet", ColonneAlignement.Gauche),
        new("Rôle", "RoleNom", ColonneAlignement.Gauche),
        new("Actif", "Actif", ColonneAlignement.Centre)
    };
}
