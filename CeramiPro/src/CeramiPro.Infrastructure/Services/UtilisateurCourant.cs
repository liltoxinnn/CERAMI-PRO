using CeramiPro.Application.Interfaces;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Personne connectée à l'application de bureau.
///
/// Contrairement à un site web, une application Windows n'a qu'un utilisateur
/// à la fois : sa session est donc conservée pour toute la durée d'exécution
/// et renseignée à la connexion.
/// </summary>
public class UtilisateurCourant : ISessionAtelier
{
    private readonly HashSet<string> _droits = new(StringComparer.OrdinalIgnoreCase);

    public int? UtilisateurId { get; private set; }

    public string? NomUtilisateur { get; private set; }

    public string? NomComplet { get; private set; }

    public string? CodeRole { get; private set; }

    public string? NomRole { get; private set; }

    public bool EstConnecte => UtilisateurId is not null;

    public bool PossedeDroit(string codeDroit)
        => !string.IsNullOrWhiteSpace(codeDroit) && _droits.Contains(codeDroit);

    /// <summary>Ouvre la session après une authentification réussie.</summary>
    public void Ouvrir(
        int utilisateurId,
        string nomUtilisateur,
        string nomComplet,
        string codeRole,
        string nomRole,
        IEnumerable<string> droits)
    {
        UtilisateurId = utilisateurId;
        NomUtilisateur = nomUtilisateur;
        NomComplet = nomComplet;
        CodeRole = codeRole;
        NomRole = nomRole;

        _droits.Clear();
        foreach (var droit in droits)
        {
            _droits.Add(droit);
        }
    }

    /// <summary>Ferme la session : plus aucun droit n'est accordé.</summary>
    public void Fermer()
    {
        UtilisateurId = null;
        NomUtilisateur = null;
        NomComplet = null;
        CodeRole = null;
        NomRole = null;
        _droits.Clear();
    }
}
