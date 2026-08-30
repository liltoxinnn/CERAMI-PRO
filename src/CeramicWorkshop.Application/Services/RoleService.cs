using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Identity;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Identity;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>Consultation des rôles et attribution des droits.</summary>
public class RoleService : IRoleService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;

    public RoleService(IApplicationDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<IReadOnlyList<RoleDto>> ListerAsync(CancellationToken cancellationToken = default)
        => await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(
                r.Id,
                r.Code,
                r.Name,
                r.Description,
                r.IsSystem,
                r.Users.Count(u => u.IsActive),
                r.RolePermissions.Select(rp => rp.Permission.Code).ToList()))
            .ToListAsync(cancellationToken);

    public async Task<RoleDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RoleDto(
                r.Id,
                r.Code,
                r.Name,
                r.Description,
                r.IsSystem,
                r.Users.Count(u => u.IsActive),
                r.RolePermissions.Select(rp => rp.Permission.Code).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return role ?? throw NotFoundException.Pour("Rôle", id);
    }

    public async Task<IReadOnlyList<ModuleDroitsDto>> ListerDroitsParModuleAsync(CancellationToken cancellationToken = default)
    {
        var droits = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Name)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Name, p.Module))
            .ToListAsync(cancellationToken);

        return droits
            .GroupBy(p => p.Module)
            .Select(g => new ModuleDroitsDto(g.Key, g.ToList()))
            .ToList();
    }

    public async Task<RoleDto> ModifierDroitsAsync(int id, ModifierDroitsRoleRequete requete, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw NotFoundException.Pour("Rôle", id);

        if (role.Code == RoleCodes.Administrateur)
        {
            throw new BusinessRuleException(
                "Le rôle « Administrateur » possède tous les droits : il ne peut pas être restreint.");
        }

        var codesDemandes = requete.CodesDroits.Distinct().ToList();

        var droits = await _context.Permissions
            .Where(p => codesDemandes.Contains(p.Code))
            .ToListAsync(cancellationToken);

        var inconnus = codesDemandes.Except(droits.Select(p => p.Code)).ToList();
        if (inconnus.Count > 0)
        {
            throw new BusinessRuleException(
                "Certains droits sélectionnés n'existent pas.", inconnus);
        }

        var actuels = role.RolePermissions.ToList();
        var idsCibles = droits.Select(p => p.Id).ToHashSet();

        foreach (var aRetirer in actuels.Where(rp => !idsCibles.Contains(rp.PermissionId)))
        {
            _context.RolePermissions.Remove(aRetirer);
        }

        var idsExistants = actuels.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var idDroit in idsCibles.Where(idDroit => !idsExistants.Contains(idDroit)))
        {
            _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = idDroit });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(Role), role.Id.ToString(),
            $"Mise à jour des droits du rôle « {role.Name} » ({droits.Count} droit(s)).", null, cancellationToken);

        return await ObtenirAsync(role.Id, cancellationToken);
    }
}
