using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

public enum ProductImageKind
{
    [Libelle("Photo principale")] Principale = 0,
    [Libelle("Photo supplémentaire")] Supplementaire = 1,
    [Libelle("Produit terminé")] ProduitTermine = 2,
    [Libelle("Pendant la fabrication")] Fabrication = 3
}
