namespace CeramicWorkshop.Domain.Common;

/// <summary>Description d'un droit : code technique, libellé français et module d'appartenance.</summary>
public sealed record PermissionDefinition(string Code, string Nom, string Module);
