using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CeramicWorkshop.Infrastructure.Authentication;

/// <summary>Génère les jetons JWT signés portant le rôle et les droits de l'utilisateur.</summary>
public class TokenService : ITokenService
{
    /// <summary>Nom du claim portant un droit fonctionnel.</summary>
    public const string ClaimDroit = "droit";

    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Cle) || _options.Cle.Length < JwtOptions.LongueurCleMinimale)
        {
            throw new InvalidOperationException(
                "La clé de signature des jetons est absente ou trop courte. " +
                $"Renseignez « Jwt:Cle » avec au moins {JwtOptions.LongueurCleMinimale} caractères.");
        }
    }

    public (string Jeton, DateTime Expiration) CreerJetonAcces(User utilisateur, IReadOnlyList<string> droits)
    {
        var expiration = DateTime.UtcNow.AddMinutes(_options.DureeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, utilisateur.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, utilisateur.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.Name, utilisateur.UserName),
            new(ClaimTypes.GivenName, utilisateur.FullName),
            new(ClaimTypes.Role, utilisateur.Role.Code)
        };

        claims.AddRange(droits.Select(droit => new Claim(ClaimDroit, droit)));

        var cle = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Cle));
        var jeton = new JwtSecurityToken(
            issuer: _options.Emetteur,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiration,
            signingCredentials: new SigningCredentials(cle, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(jeton), expiration);
    }

    public string CreerJetonRenouvellement()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}
