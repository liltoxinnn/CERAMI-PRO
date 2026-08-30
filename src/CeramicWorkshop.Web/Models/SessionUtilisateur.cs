using CeramicWorkshop.Application.DTOs.Auth;

namespace CeramicWorkshop.Web.Models;

/// <summary>
/// Session de l'utilisateur pour l'onglet en cours : jetons et profil.
/// Les jetons ne sont jamais écrits dans le code HTML de la page.
/// </summary>
public class SessionUtilisateur
{
    public string? JetonAcces { get; private set; }
    public string? JetonRenouvellement { get; private set; }
    public DateTime? Expiration { get; private set; }
    public UtilisateurConnecteDto? Profil { get; private set; }

    public bool EstConnecte => !string.IsNullOrWhiteSpace(JetonAcces) && Profil is not null;

    /// <summary>Le jeton arrive à expiration dans moins d'une minute.</summary>
    public bool JetonBientotExpire => Expiration is null || Expiration <= DateTime.UtcNow.AddMinutes(1);

    public void Definir(ConnexionReponse reponse)
    {
        JetonAcces = reponse.JetonAcces;
        JetonRenouvellement = reponse.JetonRenouvellement;
        Expiration = reponse.ExpirationJeton;
        Profil = reponse.Utilisateur;
    }

    public void Effacer()
    {
        JetonAcces = null;
        JetonRenouvellement = null;
        Expiration = null;
        Profil = null;
    }

    public bool PossedeDroit(string codeDroit)
        => Profil?.Droits.Contains(codeDroit) == true;
}

/// <summary>Contenu conservé dans le navigateur pour retrouver la session après un rafraîchissement.</summary>
public record SessionEnregistree(
    string JetonAcces,
    string JetonRenouvellement,
    DateTime Expiration,
    UtilisateurConnecteDto Profil);
