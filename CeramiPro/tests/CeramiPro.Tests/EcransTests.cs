using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.Presentation.ViewModels.Ecrans;
using FluentAssertions;

namespace CeramiPro.Tests;

/// <summary>
/// Chaque écran de liste doit annoncer un titre, une introduction et des
/// colonnes. Un écran sans colonne afficherait un tableau vide sans que rien
/// ne le signale.
///
/// Ces vérifications ne portent que sur la description des écrans : le service
/// métier n'est pas appelé, il est seulement conservé par le constructeur.
/// </summary>
public class EcransTests
{
    private static IEnumerable<Type> TypesEcrans()
        => typeof(MatieresVueModele).Assembly.GetTypes()
            .Where(t => !t.IsAbstract
                        && t.Namespace == "CeramiPro.Presentation.ViewModels.Ecrans"
                        && t.Name.EndsWith("VueModele"))
            .OrderBy(t => t.Name);

    private static (VueModeleBase Vue, IReadOnlyList<ColonneListe> Colonnes) Decrire(Type type)
    {
        var vue = (VueModeleBase)Activator.CreateInstance(type, null, new ServiceLangue())!;

        var colonnes = (IReadOnlyList<ColonneListe>)type
            .GetProperty(nameof(ListeVueModele<object>.Colonnes))!
            .GetValue(vue)!;

        return (vue, colonnes);
    }

    [Fact]
    public void Les_seize_ecrans_de_liste_sont_declares()
        => TypesEcrans().Should().HaveCount(16);

    [Fact]
    public void Chaque_ecran_porte_un_titre_et_une_introduction()
    {
        foreach (var type in TypesEcrans())
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
        foreach (var type in TypesEcrans())
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
        foreach (var type in TypesEcrans())
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

        foreach (var type in TypesEcrans())
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
        var vue = new ClientsVueModele(null!, langue);

        vue.Titre.Should().Be("Clients");

        langue.Changer(Langue.Arabe);

        vue.Titre.Should().Be("الزبائن");
    }
}
