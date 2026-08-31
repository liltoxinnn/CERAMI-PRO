using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.Presentation.ViewModels.Ecrans;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Tests;

/// <summary>
/// Chaque écran de liste doit annoncer un titre, une introduction et des
/// colonnes. Un écran sans colonne afficherait un tableau vide sans que rien
/// ne le signale.
///
/// Ces vérifications ne portent que sur la description des écrans : le
/// service métier n'est pas appelé, il est seulement conservé par le
/// constructeur.
/// </summary>
public class EcransTests
{
    /// <summary>Tous les écrans qui présentent une liste, quel qu'en soit le module.</summary>
    public static IEnumerable<Type> TypesListes()
        => typeof(MatieresVueModele).Assembly.GetTypes()
            .Where(t => !t.IsAbstract
                        && t.Namespace == "CeramiPro.Presentation.ViewModels.Ecrans"
                        && EstListe(t))
            .OrderBy(t => t.Name);

    private static bool EstListe(Type type)
    {
        for (var courant = type.BaseType; courant is not null; courant = courant.BaseType)
        {
            if (courant.IsGenericType
                && courant.GetGenericTypeDefinition() == typeof(ListeVueModele<>))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Construit l'écran en ne fournissant que la langue : c'est tout ce
    /// dont la description des colonnes a besoin.
    /// </summary>
    private static (VueModeleBase Vue, IReadOnlyList<ColonneListe> Colonnes) Decrire(Type type)
    {
        var parametres = type.GetConstructors()[0].GetParameters()
            .Select(p => p.ParameterType == typeof(IServiceLangue)
                ? (object?)new ServiceLangue()
                : null)
            .ToArray();

        var vue = (VueModeleBase)Activator.CreateInstance(type, parametres)!;

        var colonnes = (IReadOnlyList<ColonneListe>)type
            .GetProperty(nameof(ListeVueModele<object>.Colonnes))!
            .GetValue(vue)!;

        return (vue, colonnes);
    }

    [Fact]
    public void Tous_les_ecrans_de_liste_sont_declares()
        => TypesListes().Should().HaveCountGreaterThanOrEqualTo(26);

    [Fact]
    public void Chaque_ecran_porte_un_titre_et_une_introduction()
    {
        foreach (var type in TypesListes())
        {
            var (vue, _) = Decrire(type);

            vue.Titre.Should().NotBeNullOrWhiteSpace($"l'écran {type.Name} doit avoir un titre");
            vue.Introduction.Should().NotBeNullOrWhiteSpace(
                $"l'écran {type.Name} doit expliquer à quoi il sert");
        }
    }

    [Fact]
    public void Chaque_ecran_declare_des_colonnes_completes()
    {
        foreach (var type in TypesListes())
        {
            var (_, colonnes) = Decrire(type);

            colonnes.Should().NotBeEmpty($"l'écran {type.Name} doit afficher des colonnes");
            colonnes.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.EnTete));
            colonnes.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.Propriete));
        }
    }

    [Fact]
    public void Aucun_ecran_n_affiche_deux_fois_la_meme_donnee()
    {
        foreach (var type in TypesListes())
        {
            var (_, colonnes) = Decrire(type);

            colonnes.Select(c => c.Propriete).Should().OnlyHaveUniqueItems(
                $"l'écran {type.Name} ne doit pas répéter une colonne");
        }
    }

    [Fact]
    public void Les_colonnes_de_montants_et_de_quantites_sont_alignees_a_droite()
    {
        var motsChiffres = new[] { "Total", "Montant", "Prix", "Coût", "Reste", "Stock", "Quantité" };

        foreach (var type in TypesListes())
        {
            var (_, colonnes) = Decrire(type);

            var malAlignees = colonnes
                .Where(c => motsChiffres.Any(m => c.EnTete.StartsWith(m, StringComparison.OrdinalIgnoreCase))
                            && c.Alignement != ColonneAlignement.Droite)
                .Select(c => $"{type.Name}.{c.EnTete}")
                .ToList();

            // Un nombre aligné à gauche se compare mal d'une ligne à l'autre.
            malAlignees.Should().BeEmpty(
                "ces colonnes chiffrées devraient être alignées à droite : "
                + string.Join(", ", malAlignees));
        }
    }

    [Fact]
    public void Le_titre_de_chaque_ecran_suit_la_langue()
    {
        var langue = new ServiceLangue();
        var vue = new ClientsVueModele(null!, langue, null!);

        vue.Titre.Should().Be("Clients");

        langue.Changer(Langue.Arabe);

        vue.Titre.Should().Be("الزبائن");
    }

    [Fact]
    public void Un_ecran_sans_formulaire_ne_propose_pas_d_ajouter()
    {
        // Un bouton « Ajouter » sans effet vaut moins qu'un bouton absent.
        var mouvements = new MouvementsVueModele(null!, new ServiceLangue(), null!);

        mouvements.PeutAjouter.Should().BeFalse();
        mouvements.PeutModifier.Should().BeFalse();
        mouvements.PeutSupprimer.Should().BeFalse();
    }

    [Fact]
    public void Un_ecran_avec_formulaire_propose_d_ajouter_et_de_modifier()
    {
        var clients = new ClientsVueModele(null!, new ServiceLangue(), Outils());

        clients.PeutAjouter.Should().BeTrue();
        clients.PeutModifier.Should().BeTrue();
        clients.PeutSupprimer.Should().BeTrue();
    }

    [Fact]
    public void Un_paiement_se_cree_mais_ne_se_modifie_pas()
    {
        // Corriger un encaissement passe par une annulation tracée.
        var paiements = new PaiementsVueModele(null!, new ServiceLangue(), Outils());

        paiements.PeutAjouter.Should().BeTrue();
        paiements.PeutModifier.Should().BeFalse();
    }

    private static OutilsListe Outils()
        => new(new FormulaireFactice(), new DialogueFactice(), new FichierFactice(),
            null!, new ServiceCollection().BuildServiceProvider());
}
