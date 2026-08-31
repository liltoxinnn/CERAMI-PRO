using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

public enum UnitType
{
    [Libelle("Poids")] Poids = 0,
    [Libelle("Volume")] Volume = 1,
    [Libelle("Longueur")] Longueur = 2,
    [Libelle("Surface")] Surface = 3,
    [Libelle("Quantité")] Quantite = 4
}
