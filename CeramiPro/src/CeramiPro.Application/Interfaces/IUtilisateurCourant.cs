using CeramiPro.Domain.Enums;

namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Personne connectée au logiciel. Sert à tracer les opérations et à
/// vérifier les droits dans la couche métier — jamais uniquement dans
/// l'écran, qui ne fait que masquer un bouton.
/// </summary>
public interface IUtilisateurCourant
{
    int? UtilisateurId { get; }

    string? NomUtilisateur { get; }

    /// <summary>Nom complet de la personne, affiché dans l'en-tête.</summary>
    string? NomComplet { get; }

    RoleUtilisateur? Role { get; }

    /// <summary>Nom du rôle tel qu'il s'affiche : « Administrateur », « Employé ».</summary>
    string? NomRole { get; }

    bool EstConnecte { get; }

    /// <summary>
    /// Vrai pour un administrateur. C'est la seule distinction que fait le
    /// logiciel : elle ouvre les rapports, les dépenses et les paramètres.
    /// </summary>
    bool EstAdministrateur { get; }
}
