using System.Linq.Expressions;
using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Identity;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Identity;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>Création et suivi des comptes utilisateurs de l'atelier.</summary>
public class UtilisateurService : IUtilisateurService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasherService _hachage;
    private readonly ICurrentUserService _utilisateurCourant;
    private readonly IAuditService _audit;

    public UtilisateurService(
        IApplicationDbContext context,
        IPasswordHasherService hachage,
        ICurrentUserService utilisateurCourant,
        IAuditService audit)
    {
        _context = context;
        _hachage = hachage;
        _utilisateurCourant = utilisateurCourant;
        _audit = audit;
    }

    public async Task<PagedResult<UtilisateurDto>> ListerAsync(PagedRequest requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = _context.Users.Include(u => u.Role).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(u =>
                u.UserName.ToLower().Contains(recherche) ||
                u.FullName.ToLower().Contains(recherche) ||
                (u.Email != null && u.Email.ToLower().Contains(recherche)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(recherche)));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var elements = await requeteBase
            .OrderBy(u => u.FullName)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new PagedResult<UtilisateurDto>(elements, total, requete.Page, requete.TaillePage);
    }

    public async Task<UtilisateurDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
    {
        var utilisateur = await _context.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return utilisateur ?? throw NotFoundException.Pour("Utilisateur", id);
    }

    public async Task<UtilisateurDto> CreerAsync(CreerUtilisateurRequete requete, CancellationToken cancellationToken = default)
    {
        var nomUtilisateur = requete.NomUtilisateur.Trim();

        if (await _context.Users.AnyAsync(u => u.UserName.ToLower() == nomUtilisateur.ToLower(), cancellationToken))
        {
            throw new BusinessRuleException($"Le nom d'utilisateur « {nomUtilisateur} » est déjà utilisé.");
        }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == requete.RoleId, cancellationToken)
                   ?? throw new BusinessRuleException("Le rôle sélectionné n'existe pas.");

        var utilisateur = new User
        {
            UserName = nomUtilisateur,
            FullName = requete.NomComplet.Trim(),
            Email = string.IsNullOrWhiteSpace(requete.Email) ? null : requete.Email.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(requete.Telephone) ? null : requete.Telephone.Trim(),
            PasswordHash = _hachage.Hacher(requete.MotDePasse),
            RoleId = role.Id,
            IsActive = requete.Actif,
            MustChangePassword = requete.DoitChangerMotDePasse
        };

        _context.Users.Add(utilisateur);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(User), utilisateur.Id.ToString(),
            $"Création de l'utilisateur « {utilisateur.UserName} » (rôle : {role.Name}).", null, cancellationToken);

        utilisateur.Role = role;
        return Convertir(utilisateur);
    }

    public async Task<UtilisateurDto> ModifierAsync(int id, ModifierUtilisateurRequete requete, CancellationToken cancellationToken = default)
    {
        var utilisateur = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                          ?? throw NotFoundException.Pour("Utilisateur", id);

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == requete.RoleId, cancellationToken)
                   ?? throw new BusinessRuleException("Le rôle sélectionné n'existe pas.");

        var changeaitDeRole = utilisateur.RoleId != role.Id;

        if ((changeaitDeRole || !requete.Actif) && utilisateur.Role.Code == RoleCodes.Administrateur)
        {
            await VerifierDernierAdministrateurAsync(utilisateur.Id, cancellationToken);
        }

        utilisateur.FullName = requete.NomComplet.Trim();
        utilisateur.Email = string.IsNullOrWhiteSpace(requete.Email) ? null : requete.Email.Trim();
        utilisateur.PhoneNumber = string.IsNullOrWhiteSpace(requete.Telephone) ? null : requete.Telephone.Trim();
        utilisateur.RoleId = role.Id;
        utilisateur.IsActive = requete.Actif;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(User), utilisateur.Id.ToString(),
            $"Modification de l'utilisateur « {utilisateur.UserName} ».", null, cancellationToken);

        utilisateur.Role = role;
        return Convertir(utilisateur);
    }

    public async Task ReinitialiserMotDePasseAsync(int id, ReinitialiserMotDePasseRequete requete, CancellationToken cancellationToken = default)
    {
        var utilisateur = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                          ?? throw NotFoundException.Pour("Utilisateur", id);

        utilisateur.PasswordHash = _hachage.Hacher(requete.NouveauMotDePasse);
        utilisateur.MustChangePassword = requete.DoitChangerMotDePasse;
        utilisateur.FailedLoginAttempts = 0;
        utilisateur.LockedUntil = null;
        utilisateur.RefreshToken = null;
        utilisateur.RefreshTokenExpiresAt = null;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(User), utilisateur.Id.ToString(),
            $"Réinitialisation du mot de passe de « {utilisateur.UserName} ».", null, cancellationToken);
    }

    public async Task ChangerActivationAsync(int id, bool actif, CancellationToken cancellationToken = default)
    {
        var utilisateur = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                          ?? throw NotFoundException.Pour("Utilisateur", id);

        if (!actif)
        {
            if (_utilisateurCourant.UserId == id)
            {
                throw new BusinessRuleException("Vous ne pouvez pas désactiver votre propre compte.");
            }

            if (utilisateur.Role.Code == RoleCodes.Administrateur)
            {
                await VerifierDernierAdministrateurAsync(utilisateur.Id, cancellationToken);
            }
        }

        utilisateur.IsActive = actif;
        if (!actif)
        {
            utilisateur.RefreshToken = null;
            utilisateur.RefreshTokenExpiresAt = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(User), utilisateur.Id.ToString(),
            actif
                ? $"Activation du compte « {utilisateur.UserName} »."
                : $"Désactivation du compte « {utilisateur.UserName} ».",
            null, cancellationToken);
    }

    /// <summary>Empêche de laisser le logiciel sans aucun administrateur actif.</summary>
    private async Task VerifierDernierAdministrateurAsync(int idExclu, CancellationToken cancellationToken)
    {
        var autresAdministrateurs = await _context.Users
            .CountAsync(u => u.Id != idExclu && u.IsActive && u.Role.Code == RoleCodes.Administrateur, cancellationToken);

        if (autresAdministrateurs == 0)
        {
            throw new BusinessRuleException(
                "Impossible : le logiciel doit conserver au moins un administrateur actif.");
        }
    }

    /// <summary>Projection traduite en SQL par Entity Framework Core.</summary>
    private static readonly Expression<Func<User, UtilisateurDto>> Projection = u => new UtilisateurDto(
        u.Id,
        u.UserName,
        u.FullName,
        u.Email,
        u.PhoneNumber,
        u.RoleId,
        u.Role.Name,
        u.IsActive,
        u.MustChangePassword,
        u.LastLoginAt,
        u.CreatedAt);

    private static readonly Func<User, UtilisateurDto> Convertir = Projection.Compile();
}
