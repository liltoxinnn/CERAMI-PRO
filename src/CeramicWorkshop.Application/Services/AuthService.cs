using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Auth;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Entities.Identity;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Connexion, renouvellement de jeton et changement de mot de passe.
/// Après plusieurs tentatives infructueuses, le compte est bloqué temporairement.
/// </summary>
public class AuthService : IAuthService
{
    /// <summary>Nombre d'échecs successifs avant blocage temporaire du compte.</summary>
    public const int TentativesAvantBlocage = 5;

    /// <summary>Durée du blocage, en minutes.</summary>
    public const int DureeBlocageMinutes = 15;

    private const string MessageIdentifiantsInvalides = "Nom d'utilisateur ou mot de passe incorrect.";

    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasherService _hachage;
    private readonly ITokenService _jetons;
    private readonly ICurrentUserService _utilisateurCourant;
    private readonly IDateTimeService _horloge;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthService> _journal;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasherService hachage,
        ITokenService jetons,
        ICurrentUserService utilisateurCourant,
        IDateTimeService horloge,
        IAuditService audit,
        ILogger<AuthService> journal)
    {
        _context = context;
        _hachage = hachage;
        _jetons = jetons;
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
        _audit = audit;
        _journal = journal;
    }

    /// <summary>Durée de validité du jeton de renouvellement, en jours.</summary>
    private const int DureeRenouvellementJours = 7;

    public async Task<Result<ConnexionReponse>> ConnexionAsync(ConnexionRequete requete, CancellationToken cancellationToken = default)
    {
        var nom = (requete.NomUtilisateur ?? string.Empty).Trim();

        var utilisateur = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserName.ToLower() == nom.ToLower(), cancellationToken);

        if (utilisateur is null)
        {
            // Message volontairement identique pour ne pas révéler l'existence d'un compte.
            await _audit.EnregistrerAsync(AuditAction.EchecConnexion, nameof(User), null,
                $"Tentative de connexion avec un nom d'utilisateur inconnu : « {nom} ».", null, cancellationToken);
            return Result<ConnexionReponse>.Echec(MessageIdentifiantsInvalides);
        }

        var maintenant = _horloge.UtcNow;

        if (utilisateur.LockedUntil.HasValue && utilisateur.LockedUntil.Value > maintenant)
        {
            var minutes = (int)Math.Ceiling((utilisateur.LockedUntil.Value - maintenant).TotalMinutes);
            return Result<ConnexionReponse>.Echec(
                $"Compte temporairement bloqué après plusieurs tentatives. Réessayez dans {minutes} minute(s).");
        }

        if (!utilisateur.IsActive)
        {
            return Result<ConnexionReponse>.Echec("Ce compte est désactivé. Contactez l'administrateur.");
        }

        if (!_hachage.Verifier(requete.MotDePasse ?? string.Empty, utilisateur.PasswordHash))
        {
            utilisateur.FailedLoginAttempts++;

            if (utilisateur.FailedLoginAttempts >= TentativesAvantBlocage)
            {
                utilisateur.LockedUntil = maintenant.AddMinutes(DureeBlocageMinutes);
                utilisateur.FailedLoginAttempts = 0;
                _journal.LogWarning("Compte {Utilisateur} bloqué après {Tentatives} échecs de connexion.",
                    utilisateur.UserName, TentativesAvantBlocage);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _audit.EnregistrerAsync(AuditAction.EchecConnexion, nameof(User), utilisateur.Id.ToString(),
                $"Mot de passe incorrect pour « {utilisateur.UserName} ».", null, cancellationToken);

            return Result<ConnexionReponse>.Echec(MessageIdentifiantsInvalides);
        }

        var droits = await ObtenirDroitsDuRoleAsync(utilisateur.RoleId, cancellationToken);
        var (jeton, expiration) = _jetons.CreerJetonAcces(utilisateur, droits);

        utilisateur.FailedLoginAttempts = 0;
        utilisateur.LockedUntil = null;
        utilisateur.LastLoginAt = maintenant;
        utilisateur.RefreshToken = _jetons.CreerJetonRenouvellement();
        utilisateur.RefreshTokenExpiresAt = maintenant.AddDays(DureeRenouvellementJours);

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Connexion, nameof(User), utilisateur.Id.ToString(),
            $"Connexion de « {utilisateur.UserName} ».", null, cancellationToken);

        return Result<ConnexionReponse>.Reussi(
            new ConnexionReponse(jeton, expiration, utilisateur.RefreshToken!, Projeter(utilisateur, droits)));
    }

    public async Task<Result<ConnexionReponse>> RenouvelerAsync(RenouvellementRequete requete, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requete.JetonRenouvellement))
        {
            return Result<ConnexionReponse>.Echec("Session expirée. Veuillez vous reconnecter.");
        }

        var utilisateur = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.RefreshToken == requete.JetonRenouvellement, cancellationToken);

        var maintenant = _horloge.UtcNow;

        if (utilisateur is null || !utilisateur.IsActive
            || utilisateur.RefreshTokenExpiresAt is null || utilisateur.RefreshTokenExpiresAt <= maintenant)
        {
            return Result<ConnexionReponse>.Echec("Session expirée. Veuillez vous reconnecter.");
        }

        var droits = await ObtenirDroitsDuRoleAsync(utilisateur.RoleId, cancellationToken);
        var (jeton, expiration) = _jetons.CreerJetonAcces(utilisateur, droits);

        utilisateur.RefreshToken = _jetons.CreerJetonRenouvellement();
        utilisateur.RefreshTokenExpiresAt = maintenant.AddDays(DureeRenouvellementJours);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<ConnexionReponse>.Reussi(
            new ConnexionReponse(jeton, expiration, utilisateur.RefreshToken!, Projeter(utilisateur, droits)));
    }

    public async Task<Result> DeconnexionAsync(CancellationToken cancellationToken = default)
    {
        var id = _utilisateurCourant.UserId;
        if (id is null)
        {
            return Result.Reussi("Vous êtes déconnecté.");
        }

        var utilisateur = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (utilisateur is not null)
        {
            utilisateur.RefreshToken = null;
            utilisateur.RefreshTokenExpiresAt = null;
            await _context.SaveChangesAsync(cancellationToken);
            await _audit.EnregistrerAsync(AuditAction.Deconnexion, nameof(User), utilisateur.Id.ToString(),
                $"Déconnexion de « {utilisateur.UserName} ».", null, cancellationToken);
        }

        return Result.Reussi("Vous êtes déconnecté.");
    }

    public async Task<Result> ChangerMotDePasseAsync(ChangementMotDePasseRequete requete, CancellationToken cancellationToken = default)
    {
        var id = _utilisateurCourant.UserId
                 ?? throw new ForbiddenException("Vous devez être connecté pour changer votre mot de passe.");

        var utilisateur = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                          ?? throw NotFoundException.Pour("Utilisateur", id);

        if (!_hachage.Verifier(requete.MotDePasseActuel ?? string.Empty, utilisateur.PasswordHash))
        {
            return Result.Echec("Le mot de passe actuel est incorrect.");
        }

        if (requete.NouveauMotDePasse != requete.ConfirmationMotDePasse)
        {
            return Result.Echec("Le nouveau mot de passe et sa confirmation ne correspondent pas.");
        }

        utilisateur.PasswordHash = _hachage.Hacher(requete.NouveauMotDePasse);
        utilisateur.MustChangePassword = false;
        utilisateur.RefreshToken = null;
        utilisateur.RefreshTokenExpiresAt = null;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(User), utilisateur.Id.ToString(),
            "Changement de mot de passe.", null, cancellationToken);

        return Result.Reussi("Mot de passe modifié.");
    }

    public async Task<UtilisateurConnecteDto?> ObtenirProfilAsync(CancellationToken cancellationToken = default)
    {
        var id = _utilisateurCourant.UserId;
        if (id is null)
        {
            return null;
        }

        var utilisateur = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (utilisateur is null)
        {
            return null;
        }

        var droits = await ObtenirDroitsDuRoleAsync(utilisateur.RoleId, cancellationToken);
        return Projeter(utilisateur, droits);
    }

    public async Task<IReadOnlyList<string>> ObtenirDroitsDuRoleAsync(int roleId, CancellationToken cancellationToken = default)
        => await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission.Code)
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);

    private static UtilisateurConnecteDto Projeter(User utilisateur, IReadOnlyList<string> droits)
        => new(
            utilisateur.Id,
            utilisateur.UserName,
            utilisateur.FullName,
            utilisateur.Email,
            utilisateur.Role.Code,
            utilisateur.Role.Name,
            droits,
            utilisateur.MustChangePassword);
}
