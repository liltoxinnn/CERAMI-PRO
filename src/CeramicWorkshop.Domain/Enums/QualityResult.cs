using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Enums;

/// <summary>Résultat du contrôle qualité (règle métier n°10).</summary>
public enum QualityResult
{
    [Libelle("Conforme")] Conforme = 0,
    [Libelle("Non conforme")] NonConforme = 1,
    [Libelle("Retouche nécessaire")] RetoucheNecessaire = 2
}
