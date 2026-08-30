using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Enums;

public enum InvoiceStatus
{
    [Libelle("Brouillon")] Brouillon = 0,
    [Libelle("Émise")] Emise = 1,
    [Libelle("Partiellement payée")] PartiellementPayee = 2,
    [Libelle("Payée")] Payee = 3,
    [Libelle("Annulée")] Annulee = 4
}
