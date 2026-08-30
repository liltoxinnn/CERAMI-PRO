namespace CeramicWorkshop.Domain.Common;

/// <summary>
/// Libellé français affiché à l'utilisateur pour une valeur d'énumération.
/// Aucun terme technique anglais ne doit apparaître dans l'interface.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class LibelleAttribute : Attribute
{
    public LibelleAttribute(string libelle) => Libelle = libelle;

    public string Libelle { get; }
}
