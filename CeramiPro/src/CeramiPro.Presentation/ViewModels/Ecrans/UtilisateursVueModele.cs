using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Identity;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Comptes du personnel de l'atelier et rôle attribué à chacun.</summary>
public partial class UtilisateursVueModele : ListeVueModele<UtilisateurDto>
{
    private readonly IUtilisateurService _service;

    public UtilisateursVueModele(IUtilisateurService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    protected override Type TypeFormulaire => typeof(UtilisateurFormulaireVueModele);

    public override string Titre => Langue["menu.administration.utilisateurs"];

    public override string Introduction =>
        "Comptes du personnel et rôle attribué à chacun. Un compte ne se supprime pas : il se désactive, " +
        "afin que les opérations déjà enregistrées gardent leur auteur.";

    protected override Task<PagedResult<UtilisateurDto>> LireAsync()
        => _service.ListerAsync(new PagedRequest
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>
    /// Active ou désactive le compte choisi. Désactiver vaut mieux que
    /// supprimer : l'historique conserve ainsi le nom de celui qui a agi.
    /// </summary>
    [RelayCommand]
    private async Task BasculerActivationAsync()
    {
        if (Outils is null)
        {
            return;
        }

        if (ElementSelectionne is not { } utilisateur)
        {
            Outils.Dialogue.Avertissement("Choisissez d'abord un compte dans le tableau.");
            return;
        }

        var action = utilisateur.Actif ? "désactiver" : "réactiver";

        if (!Outils.Dialogue.Confirmer(
                $"Voulez-vous {action} le compte « {utilisateur.NomUtilisateur} » ?"))
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            await _service.ChangerActivationAsync(utilisateur.Id, !utilisateur.Actif);
            await RafraichirAsync();
        });

        if (MessageErreur is not null)
        {
            Outils.Dialogue.Erreur(MessageErreur);
        }
    }

    /// <summary>Attribue un nouveau mot de passe provisoire au compte choisi.</summary>
    [RelayCommand]
    private async Task ReinitialiserMotDePasseAsync()
    {
        if (Outils is null)
        {
            return;
        }

        if (ElementSelectionne is not { } utilisateur)
        {
            Outils.Dialogue.Avertissement("Choisissez d'abord un compte dans le tableau.");
            return;
        }

        var provisoire = MotDePasseProvisoire();

        if (!Outils.Dialogue.Confirmer(
                $"Un mot de passe provisoire va être attribué à « {utilisateur.NomUtilisateur} » :\n\n" +
                $"    {provisoire}\n\n" +
                "Il devra en choisir un nouveau à sa prochaine connexion.\n" +
                "Notez-le avant de continuer : il ne sera plus affiché.",
                "Réinitialiser le mot de passe"))
        {
            return;
        }

        await ExecuterAsync(() => _service.ReinitialiserMotDePasseAsync(
            utilisateur.Id,
            new ReinitialiserMotDePasseRequete
            {
                NouveauMotDePasse = provisoire,
                DoitChangerMotDePasse = true
            }));

        if (MessageErreur is null)
        {
            Outils.Dialogue.Succes(
                $"Le mot de passe de « {utilisateur.NomUtilisateur} » est maintenant :\n\n    {provisoire}");
        }
        else
        {
            Outils.Dialogue.Erreur(MessageErreur);
        }
    }

    /// <summary>
    /// Mot de passe provisoire lisible au téléphone, mais assez varié pour
    /// satisfaire les règles de complexité : majuscule, minuscule, chiffre.
    /// </summary>
    private static string MotDePasseProvisoire()
        => "Ceramipro@" + Random.Shared.Next(1000, 9999);

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Nom d'utilisateur", "NomUtilisateur"),
        new("Nom complet", "NomComplet"),
        new("Rôle", "RoleNom"),
        new("Téléphone", "Telephone"),
        new("Dernière connexion", "DerniereConnexion", ColonneAlignement.Gauche, FormatColonne.DateHeure),
        new("Actif", "Actif", ColonneAlignement.Centre)
    };
}
