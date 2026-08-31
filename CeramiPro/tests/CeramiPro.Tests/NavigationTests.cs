using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Tests;

public class CatalogueNavigationTests
{
    [Fact]
    public void Le_menu_suit_le_chemin_de_l_atelier()
    {
        var menu = CatalogueNavigation.Construire(new ServiceLangue());

        menu.Select(e => e.CleLibelle).Should().ContainInOrder(
            "menu.tableauDeBord", "menu.stock", "menu.produits", "menu.production",
            "menu.cuisson", "menu.decoration", "menu.qualite", "menu.clients",
            "menu.commandes", "menu.fournisseurs", "menu.ventes", "menu.paiements",
            "menu.factures", "menu.depenses", "menu.rapports", "menu.parametres");
    }

    [Fact]
    public void Chaque_entree_porte_un_libelle_francais_et_une_icone()
    {
        foreach (var element in Aplatir(CatalogueNavigation.Construire(new ServiceLangue())))
        {
            element.Libelle.Should().NotBeNullOrWhiteSpace();
            element.Icone.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Les_groupes_ont_des_sous_entrees()
    {
        var menu = CatalogueNavigation.Construire(new ServiceLangue());

        menu.First(e => e.CleLibelle == "menu.stock").Enfants.Select(e => e.Libelle)
            .Should().Equal("Vue générale", "Matières premières", "Produits finis",
                "Mouvements", "Alertes");

        menu.First(e => e.CleLibelle == "menu.cuisson").Enfants.Should().HaveCount(3);
    }

    [Fact]
    public void Les_ecrans_developpes_sont_atteignables_depuis_le_menu()
    {
        var avecDestination = Aplatir(CatalogueNavigation.Construire(new ServiceLangue()))
            .Where(e => e.Destination is not null)
            .ToList();

        avecDestination.Should().HaveCountGreaterThanOrEqualTo(15);
        avecDestination.Select(e => e.Destination).Should().Contain(typeof(TableauDeBordVueModele));
    }

    [Fact]
    public void Toute_entree_sans_ecran_est_un_groupe_depliable()
    {
        // Une entrée qui n'ouvre rien et ne déplie rien serait sans effet.
        var inertes = Aplatir(CatalogueNavigation.Construire(new ServiceLangue()))
            .Where(e => e.Destination is null && !e.EstGroupe)
            .Select(e => e.CleLibelle)
            .ToList();

        inertes.Should().BeSubsetOf(new[]
        {
            "menu.stock.vueGenerale", "menu.stock.produitsFinis", "menu.stock.alertes",
            "menu.produits.categories", "menu.produits.variantes", "menu.produits.recettes",
            "menu.production.planning", "menu.production.enCours", "menu.production.historique",
            "menu.cuisson.fours", "menu.decoration.types", "menu.rapports", "menu.parametres"
        }, "seuls les écrans encore à développer peuvent être inactifs");
    }

    private static IEnumerable<ElementNavigation> Aplatir(IEnumerable<ElementNavigation> elements)
    {
        foreach (var element in elements)
        {
            yield return element;

            foreach (var enfant in Aplatir(element.Enfants))
            {
                yield return enfant;
            }
        }
    }
}

public class ServiceNavigationTests
{
    private static ServiceProvider ConstruireServices()
    {
        var services = new ServiceCollection();
        services.AddTransient<TableauDeBordVueModele>();
        services.AddSingleton<CeramiPro.Application.Interfaces.IServiceEtatBaseDeDonnees,
            EtatBaseFactice>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Aucun_ecran_n_est_affiche_au_demarrage()
        => new ServiceNavigation(ConstruireServices()).VueCourante.Should().BeNull();

    [Fact]
    public void Naviguer_ouvre_l_ecran_demande()
    {
        var navigation = new ServiceNavigation(ConstruireServices());

        navigation.Naviguer<TableauDeBordVueModele>();

        navigation.VueCourante.Should().BeOfType<TableauDeBordVueModele>();
        navigation.VueCourante!.Titre.Should().Be("Tableau de bord");
    }

    [Fact]
    public void Un_changement_d_ecran_est_signale()
    {
        var navigation = new ServiceNavigation(ConstruireServices());
        VueModeleBase? recue = null;
        navigation.VueChangee += vue => recue = vue;

        navigation.Naviguer<TableauDeBordVueModele>();

        recue.Should().BeOfType<TableauDeBordVueModele>();
    }

    [Fact]
    public void Un_type_qui_n_est_pas_un_ecran_est_refuse()
    {
        var navigation = new ServiceNavigation(ConstruireServices());

        var action = () => navigation.Naviguer(typeof(string));

        action.Should().Throw<ArgumentException>().WithMessage("*n'est pas un écran*");
    }
}

public class FenetrePrincipaleTests
{
    private static FenetrePrincipaleVueModele Construire(UtilisateurFactice utilisateur)
    {
        var services = new ServiceCollection();
        services.AddTransient<TableauDeBordVueModele>();
        services.AddSingleton<CeramiPro.Application.Interfaces.IServiceEtatBaseDeDonnees,
            EtatBaseFactice>();

        return new FenetrePrincipaleVueModele(
            new ServiceNavigation(services.BuildServiceProvider()), utilisateur, new ServiceLangue());
    }

    [Fact]
    public void Le_menu_complet_est_visible_sans_restriction_de_droits()
    {
        var fenetre = Construire(new UtilisateurFactice());

        fenetre.Menu.Should().HaveCount(16);
        fenetre.Menu.First().Libelle.Should().Be("Tableau de bord");
    }

    [Fact]
    public void Le_nom_de_l_utilisateur_connecte_est_affiche()
    {
        var fenetre = Construire(new UtilisateurFactice { NomUtilisateur = "karim" });

        fenetre.NomUtilisateur.Should().Be("karim");
    }

    [Fact]
    public void Le_menu_peut_etre_reduit_puis_redeploye()
    {
        var fenetre = Construire(new UtilisateurFactice());

        fenetre.MenuReduit.Should().BeFalse();
        fenetre.BasculerMenuCommand.Execute(null);
        fenetre.MenuReduit.Should().BeTrue();
        fenetre.BasculerMenuCommand.Execute(null);
        fenetre.MenuReduit.Should().BeFalse();
    }

    [Fact]
    public void Cliquer_sur_une_entree_disponible_ouvre_son_ecran()
    {
        var fenetre = Construire(new UtilisateurFactice());
        var tableauDeBord = fenetre.Menu.First(e => e.CleLibelle == "menu.tableauDeBord");

        fenetre.NaviguerCommand.Execute(tableauDeBord);

        fenetre.VueCourante.Should().BeOfType<TableauDeBordVueModele>();
    }

    [Fact]
    public void Cliquer_sur_un_groupe_n_ouvre_aucun_ecran()
    {
        var fenetre = Construire(new UtilisateurFactice());
        var stock = fenetre.Menu.First(e => e.CleLibelle == "menu.stock");

        fenetre.NaviguerCommand.Execute(stock);

        fenetre.VueCourante.Should().BeNull();
    }
}

public class DepliageMenuTests
{
    private static FenetrePrincipaleVueModele Construire()
    {
        var services = new ServiceCollection();
        services.AddTransient<TableauDeBordVueModele>();
        services.AddSingleton<CeramiPro.Application.Interfaces.IServiceEtatBaseDeDonnees,
            EtatBaseFactice>();

        return new FenetrePrincipaleVueModele(
            new ServiceNavigation(services.BuildServiceProvider()),
            new UtilisateurFactice(), new ServiceLangue());
    }

    [Fact]
    public void Les_groupes_sont_replies_au_depart()
    {
        var fenetre = Construire();

        fenetre.Menu.Where(e => e.EstGroupe).Should().OnlyContain(e => !e.EstDeplie);
    }

    [Fact]
    public void Cliquer_sur_un_groupe_le_deplie_puis_le_replie()
    {
        var fenetre = Construire();
        var stock = fenetre.Menu.First(e => e.CleLibelle == "menu.stock");

        fenetre.NaviguerCommand.Execute(stock);
        stock.EstDeplie.Should().BeTrue();

        fenetre.NaviguerCommand.Execute(stock);
        stock.EstDeplie.Should().BeFalse();
    }

    [Fact]
    public void Deplier_un_groupe_n_ouvre_aucun_ecran()
    {
        var fenetre = Construire();

        fenetre.NaviguerCommand.Execute(fenetre.Menu.First(e => e.CleLibelle == "menu.production"));

        fenetre.VueCourante.Should().BeNull();
    }

    [Fact]
    public void Aucune_session_ouverte_est_annonce_clairement()
    {
        var services = new ServiceCollection();
        services.AddTransient<TableauDeBordVueModele>();
        services.AddSingleton<CeramiPro.Application.Interfaces.IServiceEtatBaseDeDonnees,
            EtatBaseFactice>();

        var fenetre = new FenetrePrincipaleVueModele(
            new ServiceNavigation(services.BuildServiceProvider()),
            new UtilisateurFactice { UtilisateurId = null, NomUtilisateur = null, CodeRole = null },
            new ServiceLangue());

        fenetre.NomUtilisateur.Should().Be("Aucune session");
        fenetre.NomRole.Should().BeEmpty();
    }
}
