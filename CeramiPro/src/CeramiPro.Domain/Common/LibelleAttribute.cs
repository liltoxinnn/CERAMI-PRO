namespace CeramiPro.Domain.Common;

/// <summary>
/// Libellé français affiché à l'utilisateur pour une valeur d'énumération.
/// Le nom technique reste en anglais dans le code, jamais à l'écran.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class LibelleAttribute : Attribute
{
    public LibelleAttribute(string libelle) => Libelle = libelle;

    public string Libelle { get; }
}
