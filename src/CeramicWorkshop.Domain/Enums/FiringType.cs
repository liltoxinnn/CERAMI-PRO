using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Enums;

public enum FiringType
{
    [Libelle("Première cuisson")] PremiereCuisson = 0,
    [Libelle("Cuisson finale")] CuissonFinale = 1,
    [Libelle("Cuisson de décor")] CuissonDecor = 2
}
