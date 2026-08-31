using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

public enum KilnStatus
{
    [Libelle("Disponible")] Disponible = 0,
    [Libelle("En cuisson")] EnCuisson = 1,
    [Libelle("En maintenance")] Maintenance = 2,
    [Libelle("Hors service")] HorsService = 3
}
