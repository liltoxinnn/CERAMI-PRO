using CeramiPro.Application.DTOs.Identity;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>
/// Création et modification d'un compte.
///
/// Le mot de passe n'est demandé qu'à la création : ensuite, il se
/// réinitialise depuis la liste, ce qui évite qu'un responsable puisse lire
/// ou remplacer discrètement celui d'un collègue en modifiant sa fiche.
/// </summary>
public class UtilisateurFormulaireVueModele : FormulaireVueModele<CompteRequete>
{
    private readonly IUtilisateurService _utilisateurs;
    private readonly IRoleService _roles;

    public UtilisateurFormulaireVueModele(
        IUtilisateurService utilisateurs, IRoleService roles, IServiceLangue langue)
        : base(langue)
    {
        _utilisateurs = utilisateurs;
        _roles = roles;
        _champs = Construire(Array.Empty<OptionChamp>(), creation: true);
    }

    private IReadOnlyList<ChampFormulaire> _champs;
    private IReadOnlyList<OptionChamp> _rolesDisponibles = Array.Empty<OptionChamp>();

    public override string Titre => EstCreation ? "Nouveau compte" : "Modifier le compte";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override async Task PreparerAsync()
    {
        var roles = await _roles.ListerAsync();

        _rolesDisponibles = roles.Select(r => new OptionChamp(r.Id, r.Nom)).ToList();
        _champs = Construire(_rolesDisponibles, creation: true);

        OnPropertyChanged(nameof(Champs));
    }

    public override async Task PreparerModificationAsync(int id)
    {
        var utilisateur = await _utilisateurs.ObtenirAsync(id);

        Id = id;
        Requete = new CompteRequete
        {
            NomUtilisateur = utilisateur.NomUtilisateur,
            NomComplet = utilisateur.NomComplet,
            Email = utilisateur.Email,
            Telephone = utilisateur.Telephone,
            RoleId = utilisateur.RoleId,
            Actif = utilisateur.Actif
        };

        _champs = Construire(_rolesDisponibles, creation: false);
        OnPropertyChanged(nameof(Champs));
    }

    private static IReadOnlyList<ChampFormulaire> Construire(
        IReadOnlyList<OptionChamp> roles, bool creation)
    {
        var champs = new List<ChampFormulaire>
        {
            new("Nom d'utilisateur", nameof(CompteRequete.NomUtilisateur), TypeChamp.Texte,
                Obligatoire: true,
                Aide: creation ? "Il servira à se connecter et ne pourra plus être changé." : null),
            new("Nom complet", nameof(CompteRequete.NomComplet), TypeChamp.Texte, Obligatoire: true),
            new("Rôle", nameof(CompteRequete.RoleId), TypeChamp.Liste, Obligatoire: true, Options: roles),
            new("Email", nameof(CompteRequete.Email)),
            new("Téléphone", nameof(CompteRequete.Telephone))
        };

        if (creation)
        {
            champs.Add(new ChampFormulaire(
                "Mot de passe", nameof(CompteRequete.MotDePasse), TypeChamp.Texte, Obligatoire: true,
                Aide: "Au moins huit caractères, avec une majuscule, une minuscule et un chiffre."));

            champs.Add(new ChampFormulaire(
                "Changement obligatoire à la première connexion",
                nameof(CompteRequete.DoitChangerMotDePasse), TypeChamp.Case));
        }

        champs.Add(new ChampFormulaire("Compte actif", nameof(CompteRequete.Actif), TypeChamp.Case));

        return champs;
    }

    protected override async Task EnregistrerAsync()
    {
        if (Id is { } identifiant)
        {
            await _utilisateurs.ModifierAsync(identifiant, new ModifierUtilisateurRequete
            {
                NomComplet = Requete.NomComplet,
                Email = Requete.Email,
                Telephone = Requete.Telephone,
                RoleId = Requete.RoleId,
                Actif = Requete.Actif
            });

            return;
        }

        await _utilisateurs.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = Requete.NomUtilisateur,
            NomComplet = Requete.NomComplet,
            Email = Requete.Email,
            Telephone = Requete.Telephone,
            MotDePasse = Requete.MotDePasse,
            RoleId = Requete.RoleId,
            Actif = Requete.Actif,
            DoitChangerMotDePasse = Requete.DoitChangerMotDePasse
        });
    }
}

/// <summary>
/// Saisie d'un compte, création et modification confondues. Les deux
/// requêtes du service diffèrent ; le formulaire, lui, reste unique.
/// </summary>
public class CompteRequete
{
    public string NomUtilisateur { get; set; } = string.Empty;
    public string NomComplet { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public string MotDePasse { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool Actif { get; set; } = true;
    public bool DoitChangerMotDePasse { get; set; } = true;
}
