using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

public enum IssueSeverity
{
    [Libelle("Mineure")] Mineure = 0,
    [Libelle("Majeure")] Majeure = 1,
    [Libelle("Critique")] Critique = 2
}
