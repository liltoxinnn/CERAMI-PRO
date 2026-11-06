using CeramiPro.Application.Interfaces;

namespace CeramiPro.Application.Common;

/// <summary>
/// Ce qui est réservé à l'administrateur : les comptes des utilisateurs et
/// les sauvegardes. Le menu cache ces écrans aux employés ; les services le
/// vérifient aussi, pour que l'écran ne soit pas la seule barrière.
/// </summary>
public static class Autorisation
{
    public static void ExigerAdministrateur(IUtilisateurCourant utilisateur, string action)
    {
        if (!utilisateur.EstAdministrateur)
        {
            throw new RegleMetierException(
                $"Seul un administrateur peut {action}. Demandez-lui de s'en charger depuis son compte.");
        }
    }
}
