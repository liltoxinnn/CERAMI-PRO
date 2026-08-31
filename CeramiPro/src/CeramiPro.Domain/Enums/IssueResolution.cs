using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

public enum IssueResolution
{
    [Libelle("À décider")] ADecider = 0,
    [Libelle("Retouche")] Retouche = 1,
    [Libelle("Pièce à refaire")] ARefaire = 2,
    [Libelle("Rebut")] Rebut = 3,
    [Libelle("Accepté avec réserve")] AccepteAvecReserve = 4
}
