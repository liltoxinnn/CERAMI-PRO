using System.Reflection;

namespace CeramicWorkshop.Domain.Common;

public static class EnumExtensions
{
    private static readonly Dictionary<Enum, string> Cache = new();

    /// <summary>Retourne le libellé français d'une valeur d'énumération.</summary>
    public static string Libelle(this Enum valeur)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(valeur, out var connu))
            {
                return connu;
            }

            var champ = valeur.GetType().GetField(valeur.ToString(), BindingFlags.Public | BindingFlags.Static);
            var libelle = champ?.GetCustomAttribute<LibelleAttribute>()?.Libelle ?? valeur.ToString();
            Cache[valeur] = libelle;
            return libelle;
        }
    }

    /// <summary>Liste (valeur, libellé) utilisable directement dans les listes déroulantes.</summary>
    public static IReadOnlyList<(TEnum Valeur, string Libelle)> Libelles<TEnum>() where TEnum : struct, Enum
        => Enum.GetValues<TEnum>().Select(v => (v, ((Enum)(object)v).Libelle())).ToList();
}
