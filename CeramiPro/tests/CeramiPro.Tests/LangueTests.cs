using CeramiPro.Application.Localisation;
using FluentAssertions;

namespace CeramiPro.Tests;

public class ServiceLangueTests
{
    [Fact]
    public void Le_francais_est_la_langue_de_depart()
    {
        var service = new ServiceLangue();

        service.LangueCourante.Should().Be(Langue.Francais);
        service.Sens.Should().Be(SensEcriture.GaucheADroite);
        service["menu.tableauDeBord"].Should().Be("Tableau de bord");
    }

    [Fact]
    public void Passer_en_arabe_traduit_l_interface()
    {
        var service = new ServiceLangue();

        service.Changer(Langue.Arabe);

        service.LangueCourante.Should().Be(Langue.Arabe);
        service["menu.tableauDeBord"].Should().Be("لوحة القيادة");
        service["action.enregistrer"].Should().Be("حفظ");
    }

    [Fact]
    public void L_arabe_s_ecrit_de_droite_a_gauche()
    {
        var service = new ServiceLangue();
        service.Changer(Langue.Arabe);

        service.Sens.Should().Be(SensEcriture.DroiteAGauche);
    }

    [Fact]
    public void Le_changement_de_langue_est_signale_aux_ecrans()
    {
        var service = new ServiceLangue();
        var signale = 0;
        service.LangueChangee += () => signale++;

        service.Changer(Langue.Arabe);
        service.Changer(Langue.Francais);

        signale.Should().Be(2);
    }

    [Fact]
    public void Choisir_la_langue_deja_active_ne_signale_rien()
    {
        var service = new ServiceLangue();
        var signale = 0;
        service.LangueChangee += () => signale++;

        service.Changer(Langue.Francais);

        signale.Should().Be(0);
    }

    [Fact]
    public void Une_cle_inconnue_est_renvoyee_telle_quelle()
        => new ServiceLangue()["cle.qui.nexiste.pas"].Should().Be("cle.qui.nexiste.pas");

    [Fact]
    public void Une_cle_vide_ne_provoque_pas_d_erreur()
        => new ServiceLangue()[""].Should().BeEmpty();

    [Fact]
    public void Une_traduction_accepte_des_valeurs()
    {
        var service = new ServiceLangue();

        service.Traduire("Il reste {0} pièces", 12).Should().Be("Il reste 12 pièces");
    }

    [Fact]
    public void Le_nom_de_chaque_langue_est_ecrit_dans_sa_propre_langue()
    {
        Langue.Francais.NomNatif().Should().Be("Français");
        Langue.Arabe.NomNatif().Should().Be("العربية");
    }
}

/// <summary>
/// Une clé traduite en français mais oubliée en arabe donnerait un écran
/// incohérent. Ces vérifications rendent l'oubli impossible à ignorer.
/// </summary>
public class CouvertureTraductionsTests
{
    [Fact]
    public void Chaque_cle_francaise_a_sa_traduction_arabe()
    {
        var manquantes = Traductions.Francais.Keys
            .Where(cle => !Traductions.Arabe.ContainsKey(cle))
            .OrderBy(cle => cle)
            .ToList();

        manquantes.Should().BeEmpty(
            "ces clés sont traduites en français mais pas en arabe : "
            + string.Join(", ", manquantes));
    }

    [Fact]
    public void Aucune_cle_arabe_n_est_orpheline()
    {
        var orphelines = Traductions.Arabe.Keys
            .Where(cle => !Traductions.Francais.ContainsKey(cle))
            .OrderBy(cle => cle)
            .ToList();

        orphelines.Should().BeEmpty(
            "ces clés existent en arabe sans équivalent français : "
            + string.Join(", ", orphelines));
    }

    [Fact]
    public void Aucune_traduction_n_est_vide()
    {
        foreach (var (langue, dictionnaire) in new[]
                 {
                     ("français", Traductions.Francais),
                     ("arabe", Traductions.Arabe)
                 })
        {
            dictionnaire.Should().NotContain(
                paire => string.IsNullOrWhiteSpace(paire.Value),
                $"aucun texte {langue} ne doit être vide");
        }
    }

    [Fact]
    public void Les_textes_arabes_sont_bien_ecrits_en_arabe()
    {
        // Les clés d'interface doivent contenir de l'alphabet arabe : une
        // valeur restée en français signalerait une traduction oubliée.
        var nonTraduites = Traductions.Arabe
            .Where(paire => !paire.Value.Any(c => c >= '؀' && c <= 'ۿ'))
            .Select(paire => paire.Key)
            .ToList();

        nonTraduites.Should().BeEmpty(
            "ces valeurs ne contiennent aucun caractère arabe : "
            + string.Join(", ", nonTraduites));
    }

    [Fact]
    public void Le_dictionnaire_couvre_les_rubriques_essentielles()
    {
        foreach (var prefixe in new[] { "menu.", "action.", "message.", "commun.", "connexion." })
        {
            Traductions.Francais.Keys.Should().Contain(cle => cle.StartsWith(prefixe));
            Traductions.Arabe.Keys.Should().Contain(cle => cle.StartsWith(prefixe));
        }
    }
}

/// <summary>
/// Le menu doit se traduire sans être reconstruit, et la fenêtre doit
/// s'inverser quand l'arabe est choisi.
/// </summary>
public class MenuBilingueTests
{
    private static (CeramiPro.Presentation.ViewModels.FenetrePrincipaleVueModele Fenetre,
                    ServiceLangue Langue) Construire()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions
            .AddTransient<CeramiPro.Presentation.ViewModels.TableauDeBordVueModele>(services);
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions
            .AddSingleton<CeramiPro.Application.Interfaces.IServiceEtatBaseDeDonnees,
                Aides.EtatBaseFactice>(services);

        var langue = new ServiceLangue();

        var fenetre = new CeramiPro.Presentation.ViewModels.FenetrePrincipaleVueModele(
            new CeramiPro.Presentation.Navigation.ServiceNavigation(
                Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions
                    .BuildServiceProvider(services)),
            new Aides.UtilisateurFactice(),
            langue);

        return (fenetre, langue);
    }

    [Fact]
    public void Le_menu_s_affiche_en_francais_par_defaut()
    {
        var (fenetre, _) = Construire();

        fenetre.Menu.First().Libelle.Should().Be("Tableau de bord");
        fenetre.Menu.First(e => e.CleLibelle == "menu.ventes").Libelle.Should().Be("Ventes");
    }

    [Fact]
    public void Choisir_l_arabe_traduit_le_menu_sans_le_reconstruire()
    {
        var (fenetre, _) = Construire();
        var premiereEntree = fenetre.Menu.First();

        fenetre.ChoisirLangueCommand.Execute(Langue.Arabe);

        // La même instance porte désormais le libellé arabe.
        premiereEntree.Libelle.Should().Be("لوحة القيادة");
        fenetre.Menu.First(e => e.CleLibelle == "menu.ventes").Libelle.Should().Be("المبيعات");
    }

    [Fact]
    public void Les_sous_entrees_sont_traduites_aussi()
    {
        var (fenetre, _) = Construire();

        fenetre.ChoisirLangueCommand.Execute(Langue.Arabe);

        fenetre.Menu.First(e => e.CleLibelle == "menu.stock")
            .Enfants.First().Libelle.Should().Be("نظرة عامة");
    }

    [Fact]
    public void La_fenetre_s_inverse_en_arabe()
    {
        var (fenetre, _) = Construire();

        fenetre.Sens.Should().Be(SensEcriture.GaucheADroite);

        fenetre.ChoisirLangueCommand.Execute(Langue.Arabe);

        fenetre.Sens.Should().Be(SensEcriture.DroiteAGauche);
    }

    [Fact]
    public void Revenir_au_francais_retablit_les_libelles()
    {
        var (fenetre, _) = Construire();

        fenetre.ChoisirLangueCommand.Execute(Langue.Arabe);
        fenetre.ChoisirLangueCommand.Execute(Langue.Francais);

        fenetre.Menu.First().Libelle.Should().Be("Tableau de bord");
        fenetre.Sens.Should().Be(SensEcriture.GaucheADroite);
    }

    [Fact]
    public void Le_nom_de_l_application_suit_la_langue()
    {
        var (fenetre, _) = Construire();

        fenetre.NomApplication.Should().Be("CeramiPro");

        fenetre.ChoisirLangueCommand.Execute(Langue.Arabe);

        fenetre.NomApplication.Should().Be("سيرامي برو");
    }

    [Fact]
    public void Les_deux_langues_sont_proposees()
    {
        var (fenetre, _) = Construire();

        fenetre.Langues.Should().Equal(Langue.Francais, Langue.Arabe);
    }
}
