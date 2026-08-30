namespace CeramicWorkshop.Web.Models;

/// <summary>Entrée du menu principal.</summary>
/// <param name="Libelle">Texte affiché.</param>
/// <param name="Lien">Adresse de la page.</param>
/// <param name="Icone">Pictogramme affiché à gauche du libellé.</param>
/// <param name="DroitRequis">Droit nécessaire ; vide si l'entrée est visible par tous.</param>
public record ElementMenu(string Libelle, string Lien, string Icone, string? DroitRequis = null);
