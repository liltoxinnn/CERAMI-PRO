using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Enums;

public enum FiringBatchStatus
{
    [Libelle("Planifiée")] Planifiee = 0,
    [Libelle("En cours")] EnCours = 1,
    [Libelle("Terminée")] Terminee = 2,
    [Libelle("Annulée")] Annulee = 3
}
