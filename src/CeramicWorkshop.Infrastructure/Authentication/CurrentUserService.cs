using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CeramicWorkshop.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CeramicWorkshop.Infrastructure.Authentication;

/// <summary>Lit l'identité de l'utilisateur dans le jeton de la requête en cours.</summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accesseur;

    public CurrentUserService(IHttpContextAccessor accesseur) => _accesseur = accesseur;

    private ClaimsPrincipal? Utilisateur => _accesseur.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var valeur = Utilisateur?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                         ?? Utilisateur?.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(valeur, out var id) ? id : null;
        }
    }

    public string? UserName => Utilisateur?.FindFirstValue(ClaimTypes.Name)
                               ?? Utilisateur?.FindFirstValue(JwtRegisteredClaimNames.UniqueName);

    public string? RoleCode => Utilisateur?.FindFirstValue(ClaimTypes.Role);

    public string? IpAddress => _accesseur.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool EstAuthentifie => Utilisateur?.Identity?.IsAuthenticated == true;

    public bool PossedeDroit(string codeDroit)
        => Utilisateur?.HasClaim(TokenService.ClaimDroit, codeDroit) == true;
}
