using CeramiPro.Application.DTOs.Identity;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>
/// Création et modification d'un compte.
///
/// Le mot de passe n'est demandé qu'à la création : ensuite, il se
/// réinitialise depuis la liste, ce qui évite de remplacer discrètement
/// celui d'un collègue en modifiant sa fiche.
/// </summary>
public class UtilisateurFormulaireVueModele : FormulaireVueModele<CompteRequete>
{
    private readonly IUtilisateurService _utilisateurs;

    public UtilisateurFormulaireVueModele(IUtilisateurService utilisateurs)
    {
        _utilisateurs = utilisateurs;
        _champs = Construire(creation: true);
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => EstCreation ? "Nouveau compte" : "Modifier le compte";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override Task PreparerAsync() => Task.CompletedTask;

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
            Role = utilisateur.Role,
            Actif = utilisateur.Actif
        };

        _champs = Construire(creation: false);
        OnPropertyChanged(nameof(Champs));
    }

    private static IReadOnlyList<ChampFormulaire> Construire(bool creation)
    {
        var champs = new List<ChampFormulaire>
        {
            new("Nom d'utilisateur", nameof(CompteRequete.NomUtilisateur), TypeChamp.Texte,
                Obligatoire: true,
                Aide: creation ? "Il servira à se connecter et ne pourra plus être changé." : null),
            new("Nom complet", nameof(CompteRequete.NomComplet), TypeChamp.Texte, Obligatoire: true),
            new("Rôle", nameof(CompteRequete.Role), TypeChamp.Liste, Obligatoire: true,
                Options: OptionChamp.De<RoleUtilisateur>(),
                Aide: "L'administrateur gère en plus les comptes et les sauvegardes."),
            new("Email", nameof(CompteRequete.Email)),
            new("Téléphone", nameof(CompteRequete.Telephone))
        };

        if (creation)
        {
            champs.Add(new ChampFormulaire(
                "Mot de passe", nameof(CompteRequete.MotDePasse), TypeChamp.Texte, Obligatoire: true,
                Aide: Application.Common.RegleMotDePasse.Exigences));

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
                Role = Requete.Role,
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
            Role = Requete.Role,
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
    /// <summary>
    /// « Employé » par défaut. Le type énuméré compte : « Employé » vaut zéro,
    /// et un entier nul serait pris pour un champ obligatoire laissé vide.
    /// </summary>
    public RoleUtilisateur Role { get; set; } = RoleUtilisateur.Employe;
    public bool Actif { get; set; } = true;
    public bool DoitChangerMotDePasse { get; set; } = true;
}
