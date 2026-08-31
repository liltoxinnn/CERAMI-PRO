using CeramiPro.Application.Common;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>Mise en forme d'une colonne, identique à l'écran et à l'export.</summary>
public enum FormatColonne
{
    Texte,
    Nombre,
    Quantite,
    Montant,
    Pourcentage,
    Date,
    DateHeure,
    OuiNon
}

public enum ColonneAlignement
{
    Gauche,
    Droite,
    Centre
}

/// <summary>
/// Une colonne du tableau d'un écran de liste.
///
/// Les colonnes sont déclarées par la vue-modèle plutôt que dans le XAML :
/// un seul écran générique suffit alors pour toutes les listes, et les
/// en-têtes suivent la langue choisie.
///
/// La colonne connaît aussi sa mise en forme, ce qui garantit qu'un montant
/// s'écrit de la même façon à l'écran, dans le tableur et sur le papier.
/// </summary>
public record ColonneListe(
    string EnTete,
    string Propriete,
    ColonneAlignement Alignement = ColonneAlignement.Gauche,
    FormatColonne Format = FormatColonne.Texte,
    double Largeur = double.NaN)
{
    /// <summary>
    /// Met une valeur en forme. Un champ vide s'affiche vide plutôt qu'avec
    /// un zéro ou un tiret, qui se confondraient avec une vraie donnée.
    /// </summary>
    public string Formater(object? valeur) => valeur switch
    {
        null => string.Empty,
        bool oui => oui ? "Oui" : "Non",
        decimal nombre => Formater(nombre),
        double nombre => Formater((decimal)nombre),
        int nombre when Format is FormatColonne.Montant or FormatColonne.Pourcentage
            => Formater((decimal)nombre),
        DateTime date => Format == FormatColonne.DateHeure
            ? Formatage.DateHeure(date)
            : Formatage.Date(date),
        _ => valeur.ToString() ?? string.Empty
    };

    private string Formater(decimal valeur) => Format switch
    {
        FormatColonne.Montant => Formatage.Montant(valeur),
        FormatColonne.Pourcentage => Formatage.Pourcentage(valeur),
        FormatColonne.Quantite => Formatage.Quantite(valeur),
        FormatColonne.Nombre => valeur.ToString("N0", ParametresAtelier.Culture),
        _ => Formatage.Quantite(valeur)
    };
}
