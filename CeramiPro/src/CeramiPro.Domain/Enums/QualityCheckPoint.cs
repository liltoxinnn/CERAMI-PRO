using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

/// <summary>Points contrôlés avant l'entrée en stock des produits finis.</summary>
public enum QualityCheckPoint
{
    [Libelle("Fissures")] Fissures = 0,
    [Libelle("Forme")] Forme = 1,
    [Libelle("Couleur")] Couleur = 2,
    [Libelle("Émail")] Email = 3,
    [Libelle("Décoration")] Decoration = 4,
    [Libelle("Dimensions")] Dimensions = 5,
    [Libelle("Surface")] Surface = 6,
    [Libelle("Cuisson")] Cuisson = 7
}
