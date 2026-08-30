namespace CeramicWorkshop.Application.DTOs.Identity;

public record RoleDto(
    int Id,
    string Code,
    string Nom,
    string? Description,
    bool Systeme,
    int NombreUtilisateurs,
    IReadOnlyList<string> Droits);

public record PermissionDto(int Id, string Code, string Nom, string Module);

/// <summary>Groupe de droits présenté par module dans l'écran des rôles.</summary>
public record ModuleDroitsDto(string Module, IReadOnlyList<PermissionDto> Droits);

public class ModifierDroitsRoleRequete
{
    public List<string> CodesDroits { get; set; } = new();
}
