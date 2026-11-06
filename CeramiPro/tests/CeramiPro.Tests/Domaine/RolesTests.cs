using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;
using CeramiPro.Infrastructure.Services;
using FluentAssertions;

namespace CeramiPro.Tests.Domaine;

/// <summary>Deux rôles, et c'est le rôle lui-même qui dit ce qui est permis.</summary>
public class RolesTests
{
    [Fact]
    public void L_atelier_n_a_que_deux_roles_nommes_en_francais()
    {
        Enum.GetValues<RoleUtilisateur>().Select(r => r.Libelle())
            .Should().Equal("Employé", "Administrateur");
    }

    [Fact]
    public void Les_numeros_des_roles_ne_changent_pas()
    {
        // Ils sont enregistrés en base et dans les sauvegardes.
        ((int)RoleUtilisateur.Employe).Should().Be(0);
        ((int)RoleUtilisateur.Administrateur).Should().Be(1);
    }

    [Fact]
    public void La_session_suit_le_role_de_la_personne_connectee()
    {
        var session = new UtilisateurCourant();
        session.EstConnecte.Should().BeFalse();
        session.EstAdministrateur.Should().BeFalse();

        session.Ouvrir(2, "sara", "Sara Mansouri", RoleUtilisateur.Employe);
        session.EstConnecte.Should().BeTrue();
        session.EstAdministrateur.Should().BeFalse();
        session.NomRole.Should().Be("Employé");

        session.Fermer();
        session.Ouvrir(1, "admin", "Administrateur", RoleUtilisateur.Administrateur);
        session.EstAdministrateur.Should().BeTrue();

        session.Fermer();
        session.EstConnecte.Should().BeFalse();
        session.Role.Should().BeNull();
    }
}
