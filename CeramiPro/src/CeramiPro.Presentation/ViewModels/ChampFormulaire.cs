namespace CeramiPro.Presentation.ViewModels;

/// <summary>Nature d'un champ de saisie, qui détermine le contrôle affiché.</summary>
public enum TypeChamp
{
    Texte,
    TexteLong,
    Nombre,
    Montant,
    Date,
    Liste,
    Case
}

/// <summary>Une valeur proposée dans une liste déroulante.</summary>
public record OptionChamp(int Valeur, string Libelle)
{
    /// <summary>Les valeurs d'une énumération, avec leur libellé français.</summary>
    public static IReadOnlyList<OptionChamp> De<TEnum>() where TEnum : struct, Enum
        => Domain.Common.EnumExtensions.Libelles<TEnum>()
            .Select(e => new OptionChamp(Convert.ToInt32(e.Valeur), e.Libelle))
            .ToList();
}

/// <summary>
/// Un champ d'un formulaire de saisie.
///
/// Comme pour les colonnes des tableaux, les champs sont décrits par la
/// vue-modèle : un seul formulaire générique suffit alors pour tous les
/// modules, avec la même ergonomie et les mêmes messages partout.
/// </summary>
public record ChampFormulaire(
    string Libelle,
    string Propriete,
    TypeChamp Type = TypeChamp.Texte,
    bool Obligatoire = false,
    string? Aide = null,
    IReadOnlyList<OptionChamp>? Options = null);
