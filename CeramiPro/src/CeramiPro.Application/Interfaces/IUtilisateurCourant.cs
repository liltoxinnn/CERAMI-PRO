namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Personne connectée au logiciel. Sert à tracer les opérations et à
/// vérifier les droits dans la couche métier — jamais uniquement dans l'écran.
/// </summary>
public interface IUtilisateurCourant
{
    int? UtilisateurId { get; }

    string? NomUtilisateur { get; }

    string? CodeRole { get; }

    /// <summary>Nom du rôle tel qu'il est affiché : « Administrateur », « Caissier ».</summary>
    string? NomRole { get; }

    /// <summary>Nom complet de la personne, affiché dans l'en-tête.</summary>
    string? NomComplet { get; }

    bool EstConnecte { get; }

    bool PossedeDroit(string codeDroit);
}
