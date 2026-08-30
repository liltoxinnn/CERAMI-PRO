using System.Security.Claims;
using CeramicWorkshop.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace CeramicWorkshop.Web.Services;

/// <summary>
/// Fournit à Blazor l'identité de l'utilisateur connecté ainsi que ses droits,
/// afin de n'afficher que les fonctions qui lui sont autorisées.
/// </summary>
public class FournisseurEtatAuthentification : AuthenticationStateProvider
{
    /// <summary>Nom du claim portant un droit fonctionnel.</summary>
    public const string ClaimDroit = "droit";

    private static readonly AuthenticationState Anonyme =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly ServiceAuthentification _authentification;
    private readonly SessionUtilisateur _session;
    private bool _restaurationTentee;

    public FournisseurEtatAuthentification(ServiceAuthentification authentification, SessionUtilisateur session)
    {
        _authentification = authentification;
        _session = session;
        _authentification.EtatModifie += Rafraichir;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_session.EstConnecte && !_restaurationTentee)
        {
            _restaurationTentee = true;

            try
            {
                await _authentification.RestaurerAsync();
            }
            catch (InvalidOperationException)
            {
                // Le stockage du navigateur n'est pas encore accessible (rendu initial).
                _restaurationTentee = false;
            }
        }

        return _session.Profil is null ? Anonyme : new AuthenticationState(ConstruireIdentite(_session.Profil));
    }

    private void Rafraichir()
    {
        _restaurationTentee = _session.EstConnecte;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static ClaimsPrincipal ConstruireIdentite(Application.DTOs.Auth.UtilisateurConnecteDto profil)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, profil.Id.ToString()),
            new(ClaimTypes.Name, profil.NomUtilisateur),
            new(ClaimTypes.GivenName, profil.NomComplet),
            new(ClaimTypes.Role, profil.RoleCode)
        };

        claims.AddRange(profil.Droits.Select(droit => new Claim(ClaimDroit, droit)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "CeramiPro"));
    }
}
