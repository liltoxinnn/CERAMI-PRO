using System.Windows.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Une action propre à un écran, affichée à côté d'« Ajouter » et de
/// « Modifier ».
///
/// Réceptionner un achat, défourner une cuisson, annuler une vente : ces
/// gestes n'ont rien de commun, mais tous s'appliquent à la ligne choisie
/// dans le tableau. Les décrire ici permet à l'écran de liste générique de
/// les présenter sans rien savoir du métier.
/// </summary>
public record ActionListe(
    string Libelle,
    ICommand Commande,
    bool Destructive = false,
    string? Aide = null);
