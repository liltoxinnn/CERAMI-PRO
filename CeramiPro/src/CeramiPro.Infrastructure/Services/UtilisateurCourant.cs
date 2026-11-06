using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Personne connectée à l'application de bureau.
///
/// Contrairement à un site web, une application Windows n'a qu'un utilisateur
/// à la fois : sa session est donc conservée pour toute la durée d'exécution
/// et renseignée à la connexion.
///
/// Les droits ne sont plus une liste : l'atelier n'a que deux rôles, et c'est
/// le rôle lui-même qui dit ce qui est permis.
/// </summary>
public class UtilisateurCourant : ISessionAtelier
{
    public int? UtilisateurId { get; private set; }

    public string? NomUtilisateur { get; private set; }

    public string? NomComplet { get; private set; }

    public RoleUtilisateur? Role { get; private set; }

    public string? NomRole => Role?.Libelle();

    public bool EstConnecte => UtilisateurId is not null;

    public bool EstAdministrateur => Role == RoleUtilisateur.Administrateur;

    /// <summary>Ouvre la session après une authentification réussie.</summary>
    public void Ouvrir(int utilisateurId, string nomUtilisateur, string nomComplet, RoleUtilisateur role)
    {
        UtilisateurId = utilisateurId;
        NomUtilisateur = nomUtilisateur;
        NomComplet = nomComplet;
        Role = role;
    }

    /// <summary>Ferme la session : plus aucune action n'est autorisée.</summary>
    public void Fermer()
    {
        UtilisateurId = null;
        NomUtilisateur = null;
        NomComplet = null;
        Role = null;
    }
}
