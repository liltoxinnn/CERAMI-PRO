using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

public enum PurchaseStatus
{
    [Libelle("Brouillon")] Brouillon = 0,
    [Libelle("Confirmé")] Confirme = 1,
    [Libelle("Partiellement reçu")] PartiellementRecu = 2,
    [Libelle("Reçu")] Recu = 3,
    [Libelle("Annulé")] Annule = 4
}
