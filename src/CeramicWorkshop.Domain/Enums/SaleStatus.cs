using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Enums;

public enum SaleStatus
{
    [Libelle("Brouillon")] Brouillon = 0,
    [Libelle("Confirmée")] Confirmee = 1,
    [Libelle("Annulée")] Annulee = 2
}
