using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Auth;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Application.Services;

/// <summary>
/// Ouverture de session de l'application de bureau.
///
/// Trois précautions valent d'être signalées :
/// le message d'échec est identique que le compte existe ou non, pour ne rien
/// révéler ; le compte se bloque temporairement après plusieurs essais ; et
/// chaque tentative, réussie ou non, est inscrite au journal.
/// </summary>
public class AuthService : IAuthService
{
    /// <summary>Nombre d'essais avant blocage temporaire.</summary>
    public const int TentativesAvantBlocage = 5;

    /// <summary>Durée du blocage, en minutes.</summary>
    public const int DureeBlocageMinutes = 15;

    /// <summary>
    /// Message unique pour un nom inconnu comme pour un mot de passe erroné :
    /// distinguer les deux permettrait de deviner les comptes existants.
    /// </summary>
    public const string MessageIdentifiantsInvalides =
        "Nom d'utilisateur ou mot de passe incorrect.";

    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasherService _hachage;
    private readonly IServiceDateHeure _horloge;
    private readonly IAuditService _audit;
    private readonly ISessionAtelier _session;
    private readonly ILogger<AuthService> _journal;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasherService hachage,
        IServiceDateHeure horloge,
        IAuditService audit,
        ISessionAtelier session,
        ILogger<AuthService> journal)
    {
        _context = context;
        _hachage = hachage;
        _horloge = horloge;
        _audit = audit;
        _session = session;
        _journal = journal;
    }

    public async Task<ConnexionReponse> ConnecterAsync(
        ConnexionRequete requete, CancellationToken cancellationToken = default)
    {
        var nom = (requete.NomUtilisateur ?? string.Empty).Trim();

        var utilisateur = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserName.ToLower() == nom.ToLower(), cancellationToken);

        if (utilisateur is null)
        {
            await _audit.EnregistrerAsync(AuditAction.EchecConnexion, nameof(User), null,
                $"Tentative de connexion avec un nom d'utilisateur inconnu : « {nom} ».",
                null, cancellationToken);

            throw new RegleMetierException(MessageIdentifiantsInvalides);
        }

        var maintenant = _horloge.MaintenantUtc;

        if (utilisateur.LockedUntil is { } blocage && blocage > maintenant)
        {
            var minutes = (int)Math.Ceiling((blocage - maintenant).TotalMinutes);

            throw new RegleMetierException(
                "Compte temporairement bloqué après plusieurs tentatives. " +
                $"Réessayez dans {minutes} minute(s).");
        }

        if (!utilisateur.IsActive)
        {
            throw new RegleMetierException("Ce compte est désactivé. Contactez l'administrateur.");
        }

        if (!_hachage.Verifier(requete.MotDePasse ?? string.Empty, utilisateur.PasswordHash))
        {
            await EnregistrerEchecAsync(utilisateur, maintenant, cancellationToken);
            throw new RegleMetierException(MessageIdentifiantsInvalides);
        }

        var droits = await DroitsDuRoleAsync(utilisateur.RoleId, cancellationToken);

        utilisateur.FailedLoginAttempts = 0;
        utilisateur.LockedUntil = null;
        utilisateur.LastLoginAt = maintenant;

        await _context.SaveChangesAsync(cancellationToken);

        _session.Ouvrir(
            utilisateur.Id,
            utilisateur.UserName,
            utilisateur.FullName,
            utilisateur.Role!.Code,
            utilisateur.Role.Name,
            droits);

        await _audit.EnregistrerAsync(AuditAction.Connexion, nameof(User), utilisateur.Id.ToString(),
            $"Connexion de « {utilisateur.UserName} ».", null, cancellationToken);

        return new ConnexionReponse(new UtilisateurConnecteDto(
            utilisateur.Id,
            utilisateur.UserName,
            utilisateur.FullName,
            utilisateur.Email,
            utilisateur.Role.Code,
            utilisateur.Role.Name,
            droits,
            utilisateur.MustChangePassword));
    }

    public async Task DeconnecterAsync(CancellationToken cancellationToken = default)
    {
        if (_session.EstConnecte)
        {
            await _audit.EnregistrerAsync(AuditAction.Deconnexion, nameof(User),
                _session.UtilisateurId?.ToString(),
                $"Déconnexion de « {_session.NomUtilisateur} ».", null, cancellationToken);
        }

        _session.Fermer();
    }

    public async Task ChangerMotDePasseAsync(
        ChangementMotDePasseRequete requete, CancellationToken cancellationToken = default)
    {
        if (!_session.EstConnecte)
        {
            throw new RegleMetierException("Aucune session ouverte.");
        }

        if (requete.NouveauMotDePasse != requete.ConfirmationMotDePasse)
        {
            throw new RegleMetierException(
                "Le nouveau mot de passe et sa confirmation ne correspondent pas.");
        }

        var utilisateur = await _context.Users
                              .FirstOrDefaultAsync(u => u.Id == _session.UtilisateurId, cancellationToken)
                          ?? throw new RegleMetierException("Aucune session ouverte.");

        if (!_hachage.Verifier(requete.MotDePasseActuel ?? string.Empty, utilisateur.PasswordHash))
        {
            throw new RegleMetierException("Le mot de passe actuel est incorrect.");
        }

        utilisateur.PasswordHash = _hachage.Hacher(requete.NouveauMotDePasse);
        utilisateur.MustChangePassword = false;

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(User), utilisateur.Id.ToString(),
            $"Changement de mot de passe de « {utilisateur.UserName} ».", null, cancellationToken);
    }

    private async Task EnregistrerEchecAsync(
        User utilisateur, DateTime maintenant, CancellationToken cancellationToken)
    {
        utilisateur.FailedLoginAttempts++;

        if (utilisateur.FailedLoginAttempts >= TentativesAvantBlocage)
        {
            utilisateur.LockedUntil = maintenant.AddMinutes(DureeBlocageMinutes);
            utilisateur.FailedLoginAttempts = 0;

            _journal.LogWarning(
                "Compte {Utilisateur} bloqué après {Tentatives} échecs de connexion.",
                utilisateur.UserName, TentativesAvantBlocage);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.EchecConnexion, nameof(User),
            utilisateur.Id.ToString(),
            $"Mot de passe incorrect pour « {utilisateur.UserName} ».", null, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> DroitsDuRoleAsync(
        int roleId, CancellationToken cancellationToken)
        => await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission!.Code)
            .ToListAsync(cancellationToken);
}
