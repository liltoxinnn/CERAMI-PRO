using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

public enum DecorationStatus
{
    [Libelle("Planifiée")] Planifiee = 0,
    [Libelle("En cours")] EnCours = 1,
    [Libelle("Terminée")] Terminee = 2,
    [Libelle("Annulée")] Annulee = 3
}
