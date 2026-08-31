using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

public enum StockAdjustmentReason
{
    [Libelle("Inventaire physique")] Inventaire = 0,
    [Libelle("Casse")] Casse = 1,
    [Libelle("Perte")] Perte = 2,
    [Libelle("Correction de saisie")] Correction = 3,
    [Libelle("Autre")] Autre = 4
}
