using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Enums;

public enum CustomOrderImageKind
{
    [Libelle("Photo de référence")] Reference = 0,
    [Libelle("Croquis")] Croquis = 1,
    [Libelle("Pendant la fabrication")] Fabrication = 2,
    [Libelle("Pièce terminée")] PieceTerminee = 3
}
