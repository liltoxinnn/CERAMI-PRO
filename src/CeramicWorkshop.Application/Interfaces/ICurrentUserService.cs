namespace CeramicWorkshop.Application.Interfaces;

/// <summary>Utilisateur à l'origine de la requête en cours.</summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? UserName { get; }
    string? RoleCode { get; }
    string? IpAddress { get; }
    bool EstAuthentifie { get; }
    bool PossedeDroit(string codeDroit);
}
