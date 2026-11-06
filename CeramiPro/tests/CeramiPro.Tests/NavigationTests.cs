using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.Presentation.ViewModels.Ecrans;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Tests;

public class CatalogueNavigationTests
{
    [Fact]
    public void Le_menu_compte_exactement_les_sept_sections_de_l_atelier()
    {
        CatalogueNavigation.Construire().Select(e => e.Libelle).Should().Equal(
            "Tableau de bord", "Produits", "Commandes", "Clients", "Ventes", "Rapports", "Paramètres");
    }

    [Fact]
    public void Chaque_entree_porte_un_libelle_francais_et_une_icone()
    {
        foreach (var element in Aplatir(CatalogueNavigation.Construire()))
        {
            element.Libelle.Should().NotBeNullOrWhiteSpace();
            element.Icone.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Les_sections_regroupent_leurs_ecrans()
    {
        var menu = CatalogueNavigation.Construire();

        menu.Single(e => e.Libelle == "Produits").Enfants.Select(e => e.Libelle)
            .Should().Equal("Catalogue", "Stock", "Historique du stock", "Catégories");

        menu.Single(e => e.Libelle == "Ventes").Enfants.Select(e => e.Libelle)
            .Should().Equal("Nouvelle vente", "Historique des ventes", "Paiements");

        menu.Single(e => e.Libelle == "Rapports").Enfants.Select(e => e.Libelle)
            .Should().Equal("Rapports", "Dépenses");

        menu.Single(e => e.Libelle == "Paramètres").Enfants.Select(e => e.Libelle)
            .Should().Equal("Atelier", "Catégories de dépenses", "Utilisateurs", "Sauvegarde");
    }

    [Fact]
    public void Toute_entree_ouvre_un_ecran_ou_deplie_une_section()
    {
        // Une entrée qui n'ouvre rien et ne déplie rien serait sans effet.
        var inertes = Aplatir(CatalogueNavigation.Construire())
            .Where(e => e.Destination is null && !e.EstGroupe)
            .Select(e => e.Libelle)
            .ToList();

        inertes.Should().BeEmpty("ces entrées de menu n'ont aucun effet : " + string.Join(", ", inertes));
    }

    [Fact]
    public void Chaque_destination_est_bien_un_ecran()
    {
        foreach (var element in Aplatir(CatalogueNavigation.Construire()))
        {
            if (element.Destination is { } destination)
            {
                typeof(VueModeleBase).IsAssignableFrom(destination).Should().BeTrue(
                    $"la destination de « {element.Libelle} » doit être un écran");
            }
        }
    }

    [Fact]
    public void Aucun_ecran_n_est_atteignable_par_deux_entrees_differentes()
    {
        Aplatir(CatalogueNavigation.Construire())
            .Where(e => e.Destination is not null)
            .Select(e => e.Destination!)
            .Should().OnlyHaveUniqueItems("deux entrées de menu qui ouvrent le même écran prêtent à confusion");
    }

    [Fact]
    public void Seuls_les_utilisateurs_et_la_sauvegarde_sont_reserves_a_l_administrateur()
    {
        Aplatir(CatalogueNavigation.Construire())
            .Where(e => e.AdministrateurSeulement)
            .Select(e => e.Destination)
            .Should().BeEquivalentTo(new[] { typeof(UtilisateursVueModele), typeof(SauvegardeVueModele) });
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

public class ServiceNavigationTests : IDisposable
{
    private readonly InterfaceDeTest _ui = new();

    public void Dispose() => _ui.Dispose();

    private ServiceNavigation Navigation() => new(_ui.Obtenir<IServiceProvider>());

    [Fact]
    public void Aucun_ecran_n_est_affiche_au_demarrage()
        => Navigation().VueCourante.Should().BeNull();

    [Fact]
    public void Naviguer_ouvre_l_ecran_demande()
    {
        var navigation = Navigation();

        navigation.Naviguer<TableauDeBordVueModele>();

        navigation.VueCourante.Should().BeOfType<TableauDeBordVueModele>();
        navigation.VueCourante!.Titre.Should().Be("Tableau de bord");
    }

    [Fact]
    public void Un_changement_d_ecran_est_signale()
    {
        var navigation = Navigation();
        VueModeleBase? recue = null;
        navigation.VueChangee += vue => recue = vue;

        navigation.Naviguer<TableauDeBordVueModele>();

        recue.Should().BeOfType<TableauDeBordVueModele>();
    }

    [Fact]
    public void Un_type_qui_n_est_pas_un_ecran_est_refuse()
    {
        var action = () => Navigation().Naviguer(typeof(string));

        action.Should().Throw<ArgumentException>().WithMessage("*n'est pas un écran*");
    }
}

public class FenetrePrincipaleTests : IDisposable
{
    private readonly InterfaceDeTest _ui = new();

    public void Dispose() => _ui.Dispose();

    private FenetrePrincipaleVueModele Construire(UtilisateurFactice utilisateur)
        => new(new ServiceNavigation(_ui.Obtenir<IServiceProvider>()), utilisateur);

    [Fact]
    public void Un_administrateur_voit_les_sept_sections_et_tous_les_parametres()
    {
        var fenetre = Construire(new UtilisateurFactice());

        fenetre.Menu.Should().HaveCount(7);
        fenetre.Menu.First().Libelle.Should().Be("Tableau de bord");
        fenetre.Menu.Single(e => e.Libelle == "Paramètres").Enfants.Should().HaveCount(4);
    }

    [Fact]
    public void Un_employe_voit_les_sept_sections_sans_les_utilisateurs_ni_la_sauvegarde()
    {
        // Le menu ne montre que ce qui est permis ; la couche métier refuse
        // de son côté, l'écran n'est pas la seule barrière.
        var fenetre = Construire(new UtilisateurFactice { Role = RoleUtilisateur.Employe });

        fenetre.Menu.Should().HaveCount(7);

        var parametres = fenetre.Menu.Single(e => e.Libelle == "Paramètres").Enfants.Select(e => e.Libelle);
        parametres.Should().Equal("Atelier", "Catégories de dépenses");
    }

    [Fact]
    public void Le_nom_et_le_role_de_l_utilisateur_connecte_sont_affiches()
    {
        var fenetre = Construire(new UtilisateurFactice
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Saïdi",
            Role = RoleUtilisateur.Employe
        });

        // L'en-tête montre le nom de la personne, pas son identifiant.
        fenetre.NomUtilisateur.Should().Be("Karim Saïdi");
        fenetre.NomRole.Should().Be("Employé");
    }

    [Fact]
    public void Aucune_session_ouverte_est_annoncee_clairement()
    {
        var fenetre = Construire(new UtilisateurFactice
        {
            UtilisateurId = null, NomUtilisateur = null, NomComplet = null, Role = null
        });

        fenetre.NomUtilisateur.Should().Be("Aucune session");
        fenetre.NomRole.Should().BeEmpty();
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
    public void Cliquer_sur_une_entree_ouvre_son_ecran()
    {
        var fenetre = Construire(new UtilisateurFactice());

        fenetre.NaviguerCommand.Execute(fenetre.Menu.Single(e => e.Libelle == "Commandes"));

        fenetre.VueCourante.Should().BeOfType<CommandesVueModele>();
    }

    [Fact]
    public void Les_sections_sont_repliees_au_depart()
        => Construire(new UtilisateurFactice()).Menu.Where(e => e.EstGroupe)
            .Should().OnlyContain(e => !e.EstDeplie);

    [Fact]
    public void Cliquer_sur_une_section_la_deplie_puis_la_replie_sans_ouvrir_d_ecran()
    {
        var fenetre = Construire(new UtilisateurFactice());
        var produits = fenetre.Menu.Single(e => e.Libelle == "Produits");

        fenetre.NaviguerCommand.Execute(produits);
        produits.EstDeplie.Should().BeTrue();
        fenetre.VueCourante.Should().BeNull();

        fenetre.NaviguerCommand.Execute(produits);
        produits.EstDeplie.Should().BeFalse();
    }

    [Fact]
    public void Se_deconnecter_et_changer_de_mot_de_passe_passent_par_le_compte()
    {
        var compte = new CompteFactice();
        var fenetre = new FenetrePrincipaleVueModele(
            new ServiceNavigation(_ui.Obtenir<IServiceProvider>()), new UtilisateurFactice(), compte);

        fenetre.ChangerMotDePasseCommand.Execute(null);
        fenetre.DeconnecterCommand.Execute(null);

        compte.ChangementsDemandes.Should().Be(1);
        compte.Deconnexions.Should().Be(1);
    }
}
