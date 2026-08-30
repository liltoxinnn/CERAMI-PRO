using Microsoft.AspNetCore.Authorization;

namespace CeramicWorkshop.API.Authorization;

/// <summary>
/// Exige un droit précis pour accéder à l'action.
/// Exemple : <c>[DroitRequis(PermissionCodes.UtilisateursGerer)]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class DroitRequisAttribute : AuthorizeAttribute
{
    public DroitRequisAttribute(string codeDroit) => Policy = codeDroit;
}
