using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

public enum Priority
{
    [Libelle("Basse")] Basse = 0,
    [Libelle("Normale")] Normale = 1,
    [Libelle("Haute")] Haute = 2,
    [Libelle("Urgente")] Urgente = 3
}
