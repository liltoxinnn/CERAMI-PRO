using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Settings;
using CeramiPro.Domain.Enums;
using CeramiPro.Infrastructure.Authentication;
using CeramiPro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Tests.Aides;

/// <summary>
/// Base de test en mémoire, préremplie avec un administrateur et les
/// paramètres de l'atelier, afin de vérifier les règles métier sans
/// dépendre d'un serveur PostgreSQL.
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

    private static void Remplir(CeramiProDbContext contexte)
    {
        contexte.Users.Add(new User
        {
            UserName = "admin",
            FullName = "Administrateur de l'atelier",
            PasswordHash = new PasswordHasherService().Hacher(MotDePasseAdministrateur),
            Role = RoleUtilisateur.Administrateur,
            IsActive = true
        });

        contexte.BusinessSettings.Add(new BusinessSettings { WorkshopName = "CERAMIPRO" });
        contexte.SaveChanges();
    }
}
