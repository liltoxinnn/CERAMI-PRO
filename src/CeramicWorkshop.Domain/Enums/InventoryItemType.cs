using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Enums;

/// <summary>Nature de l'article concerné par un mouvement de stock.</summary>
public enum InventoryItemType
{
    [Libelle("Matière première")] MatierePremiere = 0,
    [Libelle("Produit fini")] ProduitFini = 1
}
