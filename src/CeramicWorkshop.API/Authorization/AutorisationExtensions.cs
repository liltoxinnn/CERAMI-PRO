using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Infrastructure.Authentication;

namespace CeramicWorkshop.API.Authorization;

public static class AutorisationExtensions
{
    /// <summary>
    /// Déclare une règle d'autorisation par droit du catalogue :
    /// l'utilisateur doit porter le droit correspondant dans son jeton.
    /// </summary>
    public static IServiceCollection AddAutorisationParDroits(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var droit in PermissionCodes.Catalogue)
            {
                options.AddPolicy(droit.Code, regle => regle
                    .RequireAuthenticatedUser()
                    .RequireClaim(TokenService.ClaimDroit, droit.Code));
            }

            options.FallbackPolicy = null;
        });

        return services;
    }
}
