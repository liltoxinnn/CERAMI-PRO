namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Ce qu'un écran de liste attend d'un formulaire de saisie, sans connaître
/// le type de la requête qu'il remplit.
///
/// C'est ce contrat qui permet à l'écran de liste générique d'ouvrir aussi
/// bien une fiche client qu'un ordre de production.
/// </summary>
public interface IFormulaire
{
    /// <summary>
    /// Charge ce dont le formulaire a besoin avant d'être affiché : listes
    /// déroulantes, unités, catégories. Appelé avant chaque ouverture.
    /// </summary>
    Task PreparerAsync();

    /// <summary>Reprend les valeurs d'une fiche existante pour la modifier.</summary>
    Task PreparerModificationAsync(int id);
}
