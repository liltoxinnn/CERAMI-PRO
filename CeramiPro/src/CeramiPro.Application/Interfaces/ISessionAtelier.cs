namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Session ouverte dans l'application. Elle étend la simple lecture de
/// l'utilisateur courant avec les deux opérations que seule
/// l'authentification a le droit d'appeler.
/// </summary>
public interface ISessionAtelier : IUtilisateurCourant
{
    void Ouvrir(
        int utilisateurId,
        string nomUtilisateur,
        string nomComplet,
        string codeRole,
        string nomRole,
        IEnumerable<string> droits);

    void Fermer();
}
