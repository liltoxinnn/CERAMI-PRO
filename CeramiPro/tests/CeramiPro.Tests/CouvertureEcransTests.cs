using System.Text.RegularExpressions;
using CeramiPro.Presentation;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.Presentation.ViewModels.Ecrans;
using CeramiPro.Presentation.ViewModels.Formulaires;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Tests;

/// <summary>
/// Vérifie qu'aucun écran n'est resté à mi-chemin.
///
/// Un écran écrit mais jamais associé à une vue s'afficherait comme le nom de
/// sa classe ; un écran jamais enregistré échouerait au clic. Ni l'un ni
/// l'autre n'est visible à la compilation : ces vérifications les rattrapent.
/// </summary>
public class CouvertureEcransTests
{
    /// <summary>
    /// Le fichier qui associe chaque vue-modèle à son écran. Il appartient à
    /// l'application Windows, qui ne se compile pas ici : il est donc lu
    /// comme un texte, depuis la racine du dépôt.
    /// </summary>
    private static string Gabarits()
    {
        var racine = RacineDuDepot();
        var chemin = Path.Combine(racine, "src", "CeramiPro.App", "Ressources", "Vues.xaml");

        File.Exists(chemin).Should().BeTrue($"le fichier des vues est attendu à « {chemin} »");

        return File.ReadAllText(chemin);
    }

    private static string RacineDuDepot()
    {
        var dossier = new DirectoryInfo(AppContext.BaseDirectory);

        while (dossier is not null && !File.Exists(Path.Combine(dossier.FullName, "CeramiPro.sln")))
        {
            dossier = dossier.Parent;
        }

        dossier.Should().NotBeNull("la racine du dépôt doit être retrouvable depuis les tests");

        return dossier!.FullName;
    }

    private static IEnumerable<Type> Ecrans()
        => typeof(MatieresVueModele).Assembly.GetTypes()
            .Where(t => !t.IsAbstract
                        && t.Namespace == "CeramiPro.Presentation.ViewModels.Ecrans"
                        && t.Name.EndsWith("VueModele"))
            .OrderBy(t => t.Name);

    private static IEnumerable<Type> Formulaires()
        => typeof(ClientFormulaireVueModele).Assembly.GetTypes()
            .Where(t => !t.IsAbstract
                        && t.Namespace == "CeramiPro.Presentation.ViewModels.Formulaires"
                        && typeof(IFormulaire).IsAssignableFrom(t))
            .OrderBy(t => t.Name);

    [Fact]
    public void Chaque_ecran_est_associe_a_une_vue()
    {
        var gabarits = Gabarits();

        var sansVue = Ecrans()
            .Where(t => !gabarits.Contains($"ecrans:{t.Name}}}"))
            .Select(t => t.Name)
            .ToList();

        sansVue.Should().BeEmpty(
            "ces écrans n'ont aucune vue : ils s'afficheraient comme le nom de leur classe — "
            + string.Join(", ", sansVue));
    }

    [Fact]
    public void Chaque_vue_associee_correspond_a_un_ecran_reel()
    {
        // Un gabarit qui vise une classe supprimée ferait échouer le
        // chargement de l'application entière, dès le démarrage.
        var connus = Ecrans().Select(t => t.Name)
            .Concat(new[] { nameof(TableauDeBordVueModele) })
            .ToHashSet();

        var inconnus = Regex.Matches(Gabarits(), @"\{x:Type (?:ecrans|vm):(\w+)\}")
            .Select(m => m.Groups[1].Value)
            .Where(nom => !connus.Contains(nom))
            .ToList();

        inconnus.Should().BeEmpty(
            "ces vues visent une classe qui n'existe pas : " + string.Join(", ", inconnus));
    }

    [Fact]
    public void Chaque_ecran_est_enregistre_dans_l_injection_de_dependances()
    {
        var services = new ServiceCollection();
        services.AjouterPresentation();

        var enregistres = services.Select(d => d.ServiceType).ToHashSet();

        var oublies = Ecrans()
            .Where(t => !enregistres.Contains(t))
            .Select(t => t.Name)
            .ToList();

        oublies.Should().BeEmpty(
            "ces écrans ne sont pas enregistrés : ils échoueraient au clic — "
            + string.Join(", ", oublies));
    }

    [Fact]
    public void Chaque_formulaire_est_enregistre_dans_l_injection_de_dependances()
    {
        var services = new ServiceCollection();
        services.AjouterPresentation();

        var enregistres = services.Select(d => d.ServiceType).ToHashSet();

        var oublies = Formulaires()
            .Where(t => !enregistres.Contains(t))
            .Select(t => t.Name)
            .ToList();

        oublies.Should().BeEmpty(
            "ces formulaires ne sont pas enregistrés : le bouton « Ajouter » "
            + "resterait sans effet — " + string.Join(", ", oublies));
    }

    [Fact]
    public void Aucun_ecran_ne_reste_sans_titre_ni_introduction()
    {
        // Vérifié pour les listes par ailleurs ; ici pour les écrans
        // particuliers, dont le titre s'affiche dans l'en-tête de la fenêtre.
        var particuliers = new[]
        {
            typeof(RapportsVueModele), typeof(AlertesVueModele), typeof(StockVueModele),
            typeof(ParametresVueModele), typeof(SauvegardeVueModele),
            typeof(EtiquettesVueModele), typeof(TableauProductionVueModele),
            typeof(CalculateursVueModele), typeof(CaisseVueModele),
            typeof(NouvelAchatVueModele), typeof(EnfournementVueModele)
        };

        foreach (var type in particuliers)
        {
            var parametres = type.GetConstructors()[0].GetParameters()
                .Select(p => p.ParameterType == typeof(CeramiPro.Application.Localisation.IServiceLangue)
                    ? (object?)new CeramiPro.Application.Localisation.ServiceLangue()
                    : null)
                .ToArray();

            var vue = (VueModeleBase)Activator.CreateInstance(type, parametres)!;

            vue.Titre.Should().NotBeNullOrWhiteSpace($"l'écran {type.Name} doit avoir un titre");
            vue.Introduction.Should().NotBeNullOrWhiteSpace(
                $"l'écran {type.Name} doit expliquer à quoi il sert");
        }
    }
}
