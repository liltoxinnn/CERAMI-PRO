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
            "menu.tableauDeBord", "menu.caisse", "menu.stock", "menu.produits",
            "menu.production", "menu.cuisson", "menu.decoration", "menu.qualite",
            "menu.clients", "menu.commandes", "menu.fournisseurs", "menu.ventes",
            "menu.factures", "menu.paiements", "menu.depenses", "menu.rapports",
            "menu.administration", "menu.parametres");
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
    public void Aucun_libelle_ne_reste_a_l_etat_de_cle_technique()
    {
        // Une clé sans traduction s'afficherait telle quelle dans le menu.
        var brutes = Aplatir(CatalogueNavigation.Construire(new ServiceLangue()))
            .Where(e => e.Libelle.StartsWith("menu.", StringComparison.Ordinal))
            .Select(e => e.CleLibelle)
            .ToList();

        brutes.Should().BeEmpty("ces entrées de menu n'ont pas de traduction : "
            + string.Join(", ", brutes));
    }

    [Fact]
    public void Les_groupes_ont_des_sous_entrees()
    {
        var menu = CatalogueNavigation.Construire(new ServiceLangue());

        menu.First(e => e.CleLibelle == "menu.stock").Enfants.Select(e => e.Libelle)
            .Should().Equal("Vue générale", "Matières premières", "Produits finis",
                "Mouvements", "Alertes");

        menu.First(e => e.CleLibelle == "menu.cuisson").Enfants.Should().HaveCount(3);
        menu.First(e => e.CleLibelle == "menu.administration").Enfants.Should().HaveCount(5);
    }

    [Fact]
    public void Toute_entree_ouvre_un_ecran_ou_deplie_un_groupe()
    {
        // Une entrée qui n'ouvre rien et ne déplie rien serait sans effet :
        // il n'en reste aucune, tous les écrans étant développés.
        var inertes = Aplatir(CatalogueNavigation.Construire(new ServiceLangue()))
            .Where(e => e.Destination is null && !e.EstGroupe)
            .Select(e => e.CleLibelle)
            .ToList();

        inertes.Should().BeEmpty("ces entrées de menu n'ont aucun effet : "
            + string.Join(", ", inertes));
    }

    [Fact]
    public void Chaque_destination_est_bien_un_ecran()
    {
        foreach (var element in Aplatir(CatalogueNavigation.Construire(new ServiceLangue())))
        {
            if (element.Destination is { } destination)
            {
                typeof(VueModeleBase).IsAssignableFrom(destination).Should().BeTrue(
                    $"la destination de « {element.CleLibelle} » doit être un écran");
            }
        }
    }

    [Fact]
    public void Aucun_ecran_n_est_atteignable_par_deux_entrees_differentes()
    {
        var destinations = Aplatir(CatalogueNavigation.Construire(new ServiceLangue()))
            .Where(e => e.Destination is not null)
            .Select(e => e.Destination!)
            .ToList();

        destinations.Should().OnlyHaveUniqueItems(
            "deux entrées de menu qui ouvrent le même écran prêtent à confusion");
    }

    public static IEnumerable<ElementNavigation> Aplatir(IEnumerable<ElementNavigation> elements)
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
        foreach (var droit in CeramiPro.Domain.Common.PermissionCodes.Catalogue)
        {
            utilisateur.Droits.Add(droit.Code);
        }

        var services = new ServiceCollection();
        services.AddTransient<TableauDeBordVueModele>();
        services.AddSingleton<CeramiPro.Application.Interfaces.IServiceEtatBaseDeDonnees,
            EtatBaseFactice>();

        return new FenetrePrincipaleVueModele(
            new ServiceNavigation(services.BuildServiceProvider()), utilisateur, new ServiceLangue());
    }

    [Fact]
    public void Un_administrateur_voit_le_menu_complet()
    {
        var fenetre = Construire(new UtilisateurFactice());

        fenetre.Menu.Should().HaveCount(19);
        fenetre.Menu.First().Libelle.Should().Be("Tableau de bord");
    }

    [Fact]
    public void Un_caissier_ne_voit_pas_l_administration()
    {
        // Le menu ne montre que ce à quoi la personne a droit ; la couche
        // métier refuse de son côté, l'écran n'est pas la seule barrière.
        var caissier = new UtilisateurFactice { CodeRole = "caissier", NomRole = "Caissier" };
        caissier.Droits.Add(CeramiPro.Domain.Common.PermissionCodes.TableauDeBordConsulter);
        caissier.Droits.Add(CeramiPro.Domain.Common.PermissionCodes.VentesCreer);
        caissier.Droits.Add(CeramiPro.Domain.Common.PermissionCodes.VentesConsulter);

        var services = new ServiceCollection();
        services.AddTransient<TableauDeBordVueModele>();
        services.AddSingleton<CeramiPro.Application.Interfaces.IServiceEtatBaseDeDonnees,
            EtatBaseFactice>();

        var fenetre = new FenetrePrincipaleVueModele(
            new ServiceNavigation(services.BuildServiceProvider()), caissier, new ServiceLangue());

        var cles = fenetre.Menu.Select(e => e.CleLibelle).ToList();

        cles.Should().Contain("menu.caisse");
        cles.Should().Contain("menu.ventes");
        cles.Should().NotContain("menu.administration");
        cles.Should().NotContain("menu.parametres");
        cles.Should().NotContain("menu.depenses");
    }

    [Fact]
    public void Un_groupe_dont_tout_est_interdit_disparait()
    {
        // Un groupe vide qui se déplie sur rien serait déroutant : il doit
        // disparaître, pendant que celui dont une entrée reste permise tient.
        var employe = new UtilisateurFactice { CodeRole = "employe", NomRole = "Employé" };
        employe.Droits.Add(CeramiPro.Domain.Common.PermissionCodes.TableauDeBordConsulter);
        employe.Droits.Add(CeramiPro.Domain.Common.PermissionCodes.ProductionConsulter);

        var services = new ServiceCollection();
        services.AddTransient<TableauDeBordVueModele>();
        services.AddSingleton<CeramiPro.Application.Interfaces.IServiceEtatBaseDeDonnees,
            EtatBaseFactice>();

        var fenetre = new FenetrePrincipaleVueModele(
            new ServiceNavigation(services.BuildServiceProvider()), employe, new ServiceLangue());

        var cles = fenetre.Menu.Select(e => e.CleLibelle).ToList();

        cles.Should().Contain("menu.production");
        cles.Should().NotContain("menu.stock");
        cles.Should().NotContain("menu.administration");

        fenetre.Menu.Where(e => e.EstGroupe).Should().NotBeEmpty()
            .And.OnlyContain(e => e.Enfants.Count > 0);
    }

    [Fact]
    public void Le_nom_de_l_utilisateur_connecte_est_affiche()
    {
        var fenetre = Construire(new UtilisateurFactice
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Saïdi",
            NomRole = "Responsable"
        });

        // L'en-tête montre le nom de la personne, pas son identifiant.
        fenetre.NomUtilisateur.Should().Be("Karim Saïdi");
        fenetre.NomRole.Should().Be("Responsable");
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

        var utilisateur = new UtilisateurFactice();

        foreach (var droit in CeramiPro.Domain.Common.PermissionCodes.Catalogue)
        {
            utilisateur.Droits.Add(droit.Code);
        }

        return new FenetrePrincipaleVueModele(
            new ServiceNavigation(services.BuildServiceProvider()),
            utilisateur, new ServiceLangue());
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
            new UtilisateurFactice
            {
                UtilisateurId = null, NomUtilisateur = null, NomComplet = null,
                CodeRole = null, NomRole = null
            },
            new ServiceLangue());

        fenetre.NomUtilisateur.Should().Be("Aucune session");
        fenetre.NomRole.Should().BeEmpty();
    }
}
