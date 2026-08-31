using CeramiPro.Domain.Common;

namespace CeramiPro.Application.Localisation;

/// <summary>Langues proposées par le logiciel.</summary>
public enum Langue
{
    [Libelle("Français")] Francais = 0,
    [Libelle("العربية")] Arabe = 1
}

/// <summary>Sens de lecture d'une langue.</summary>
public enum SensEcriture
{
    /// <summary>De gauche à droite : français.</summary>
    GaucheADroite = 0,

    /// <summary>De droite à gauche : arabe.</summary>
    DroiteAGauche = 1
}

/// <summary>Renseignements attachés à chaque langue.</summary>
public static class LangueInfo
{
    public static string CodeCulture(this Langue langue) => langue switch
    {
        Langue.Arabe => "ar-DZ",
        _ => "fr-DZ"
    };

    /// <summary>
    /// L'arabe s'écrit de droite à gauche : toute l'interface doit s'inverser,
    /// pas seulement les textes.
    /// </summary>
    public static SensEcriture Sens(this Langue langue) => langue switch
    {
        Langue.Arabe => SensEcriture.DroiteAGauche,
        _ => SensEcriture.GaucheADroite
    };

    /// <summary>Nom de la langue écrit dans cette langue.</summary>
    public static string NomNatif(this Langue langue) => langue switch
    {
        Langue.Arabe => "العربية",
        _ => "Français"
    };
}
