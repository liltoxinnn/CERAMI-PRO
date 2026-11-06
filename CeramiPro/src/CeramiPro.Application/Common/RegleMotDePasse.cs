namespace CeramiPro.Application.Common;

/// <summary>
/// La règle annoncée à l'écran, appliquée partout où un mot de passe est
/// choisi : création d'un compte, réinitialisation, changement par soi-même.
/// </summary>
public static class RegleMotDePasse
{
    public const string Exigences = "Au moins huit caractères, dont une majuscule, une minuscule et un chiffre.";

    public static void Verifier(string? motDePasse)
    {
        var valeur = motDePasse ?? string.Empty;

        if (valeur.Length < 8 || !valeur.Any(char.IsUpper) || !valeur.Any(char.IsLower) || !valeur.Any(char.IsDigit))
        {
            throw new RegleMetierException($"Ce mot de passe est trop faible. {Exigences}");
        }
    }
}
