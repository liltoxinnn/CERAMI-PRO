using System.Reflection;

namespace CeramiPro.Domain.Common;

/// <summary>Lecture des libellés français associés aux énumérations.</summary>
public static class EnumExtensions
{
    /// <summary>Libellé français d'une valeur, ou son nom technique à défaut.</summary>
    public static string Libelle(this Enum valeur)
    {
        var champ = valeur.GetType().GetField(valeur.ToString());

        return champ?.GetCustomAttribute<LibelleAttribute>()?.Libelle ?? valeur.ToString();
    }

    /// <summary>Toutes les valeurs d'une énumération, avec leur libellé français.</summary>
    public static IReadOnlyList<(TEnum Valeur, string Libelle)> Libelles<TEnum>()
        where TEnum : struct, Enum
        => Enum.GetValues<TEnum>()
            .Select(valeur => (valeur, ((Enum)(object)valeur).Libelle()))
            .ToList();
}
