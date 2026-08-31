using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Settings;
using CeramiPro.Infrastructure.Authentication;
using CeramiPro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Tests.Aides;

/// <summary>
/// Base de test en mémoire préremplie avec les rôles, les droits et un administrateur,
/// afin de vérifier les règles métier sans dépendre d'un serveur PostgreSQL.
/// </summary>
public static class ContexteTest
{
    public const string MotDePasseAdministrateur = "Admin@2026";

    public static CeramiProDbContext Creer(
        IUtilisateurCourant? utilisateurCourant = null,
        IServiceDateHeure? horloge = null)
    {
        var options = new DbContextOptionsBuilder<CeramiProDbContext>()
            .UseInMemoryDatabase($"ceramipro-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var contexte = new CeramiProDbContext(
            options,
            utilisateurCourant ?? new UtilisateurCourantFactice(),
            horloge ?? new HorlogeFactice());

        Remplir(contexte);
        return contexte;
    }

    /// <summary>Crée les rôles, les droits et le compte administrateur de départ.</summary>
    private static void Remplir(CeramiProDbContext contexte)
    {
        var hachage = new PasswordHasherService();

        var droits = PermissionCodes.Catalogue
            .Select(d => new Permission { Code = d.Code, Name = d.Nom, Module = d.Module })
            .ToList();

        contexte.Permissions.AddRange(droits);

        var roles = RoleCodes.Catalogue
            .Select(r => new Role { Code = r.Code, Name = r.Nom, Description = r.Description, IsSystem = true })
            .ToList();

        contexte.Roles.AddRange(roles);
        contexte.SaveChanges();

        foreach (var role in roles)
        {
            foreach (var code in PermissionCodes.DroitsParDefaut[role.Code])
            {
                var droit = droits.First(d => d.Code == code);
                contexte.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = droit.Id });
            }
        }

        var roleAdministrateur = roles.First(r => r.Code == RoleCodes.Administrateur);

        contexte.Users.Add(new User
        {
            UserName = "admin",
            FullName = "Administrateur de l'atelier",
            PasswordHash = hachage.Hacher(MotDePasseAdministrateur),
            RoleId = roleAdministrateur.Id,
            IsActive = true
        });

        contexte.BusinessSettings.Add(new BusinessSettings { WorkshopName = "CERAMIPRO" });
        contexte.SaveChanges();
    }
}
