namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Une colonne du tableau d'un écran de liste.
///
/// Les colonnes sont déclarées par la vue-modèle plutôt que dans le XAML :
/// un seul écran générique suffit alors pour toutes les listes, et les
/// en-têtes suivent la langue choisie.
/// </summary>
public record ColonneListe(
    string EnTete,
    string Propriete,
    ColonneAlignement Alignement = ColonneAlignement.Gauche,
    double Largeur = double.NaN,
    string? Format = null);

public enum ColonneAlignement
{
    Gauche,
    Droite,
    Centre
}
