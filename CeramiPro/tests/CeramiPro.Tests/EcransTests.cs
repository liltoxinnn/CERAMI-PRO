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
        => typeof(ClientsVueModele).Assembly.GetTypes()
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
    /// Construit l'écran sans aucun service : la description des colonnes
    /// n'en a pas besoin.
    /// </summary>
    private static (VueModeleBase Vue, IReadOnlyList<ColonneListe> Colonnes) Decrire(Type type)
    {
        var parametres = type.GetConstructors()[0].GetParameters()
            .Select(_ => (object?)null)
            .ToArray();

        var vue = (VueModeleBase)Activator.CreateInstance(type, parametres)!;

        var colonnes = (IReadOnlyList<ColonneListe>)type
            .GetProperty(nameof(ListeVueModele<object>.Colonnes))!
            .GetValue(vue)!;

        return (vue, colonnes);
    }

    [Fact]
    public void Tous_les_ecrans_de_liste_sont_declares()
        => TypesListes().Should().HaveCount(11);

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
    public void Chaque_colonne_lit_une_propriete_qui_existe_vraiment()
    {
        // Une colonne liée à une propriété absente resterait vide à l'écran,
        // sans aucun message : WPF échoue en silence.
        var introuvables = new List<string>();

        foreach (var type in TypesListes())
        {
            var element = TypeElement(type);
            var (_, colonnes) = Decrire(type);

            introuvables.AddRange(colonnes
                .Where(c => element.GetProperty(c.Propriete) is null)
                .Select(c => $"{type.Name} → {element.Name}.{c.Propriete}"));
        }

        introuvables.Should().BeEmpty(
            "ces colonnes visent une propriété qui n'existe pas : " + string.Join(", ", introuvables));
    }

    [Fact]
    public void Les_colonnes_de_montants_lisent_des_nombres()
    {
        var malTypees = new List<string>();

        foreach (var type in TypesListes())
        {
            var element = TypeElement(type);
            var (_, colonnes) = Decrire(type);

            foreach (var colonne in colonnes.Where(c => c.Format is FormatColonne.Montant or FormatColonne.Quantite))
            {
                var propriete = element.GetProperty(colonne.Propriete)?.PropertyType;
                var reel = propriete is null ? null : Nullable.GetUnderlyingType(propriete) ?? propriete;

                if (reel is not null && reel != typeof(decimal) && reel != typeof(int))
                {
                    malTypees.Add($"{type.Name}.{colonne.Propriete} ({reel.Name})");
                }
            }
        }

        malTypees.Should().BeEmpty("une colonne de montant doit lire un nombre : " + string.Join(", ", malTypees));
    }

    [Fact]
    public void Un_ecran_sans_formulaire_ne_propose_pas_d_ajouter()
    {
        // Un bouton « Ajouter » sans effet vaut moins qu'un bouton absent.
        var mouvements = new MouvementsVueModele(null!, Outils());

        mouvements.PeutAjouter.Should().BeFalse();
        mouvements.PeutModifier.Should().BeFalse();
        mouvements.PeutSupprimer.Should().BeFalse();
    }

    [Fact]
    public void Un_ecran_avec_formulaire_propose_d_ajouter_et_de_modifier()
    {
        var clients = new ClientsVueModele(null!, null!, Outils());

        clients.PeutAjouter.Should().BeTrue();
        clients.PeutModifier.Should().BeTrue();
    }

    [Fact]
    public void Un_paiement_se_corrige_par_une_annulation_tracee()
    {
        // Un encaissement ne se modifie pas : on l'annule, avec un motif.
        var paiements = new PaiementsVueModele(null!, null!, Outils());

        paiements.PeutModifier.Should().BeFalse();
        paiements.Actions.Should().Contain(a => a.Destructive && a.Libelle == "Annuler le paiement");
    }

    [Fact]
    public void Une_vente_ne_se_modifie_pas_et_s_annule_avec_un_motif()
    {
        var ventes = new VentesVueModele(null!, null!, Outils());

        ventes.PeutAjouter.Should().BeFalse("une vente se saisit depuis « Nouvelle vente »");
        ventes.PeutModifier.Should().BeFalse();
        ventes.Actions.Should().Contain(a => a.Destructive && a.Libelle == "Annuler la vente");
    }

    [Fact]
    public void Chaque_action_porte_un_libelle_et_une_commande()
    {
        foreach (var type in TypesListes())
        {
            var (vue, _) = Decrire(type);
            var actions = (IReadOnlyList<ActionListe>)type.GetProperty("Actions")!.GetValue(vue)!;

            foreach (var action in actions)
            {
                action.Libelle.Should().NotBeNullOrWhiteSpace($"chaque action de {type.Name} doit être nommée");
                action.Commande.Should().NotBeNull($"« {action.Libelle} » ({type.Name}) doit faire quelque chose");
            }
        }
    }

    /// <summary>Type des lignes affichées par un écran de liste.</summary>
    public static Type TypeElement(Type ecran)
    {
        for (var courant = ecran.BaseType; courant is not null; courant = courant.BaseType)
        {
            if (courant.IsGenericType && courant.GetGenericTypeDefinition() == typeof(ListeVueModele<>))
            {
                return courant.GetGenericArguments()[0];
            }
        }

        throw new InvalidOperationException($"{ecran.Name} n'est pas un écran de liste.");
    }

    private static OutilsListe Outils()
        => new(new FormulaireFactice(), new DialogueFactice(), new FichierFactice(),
            null!, new FenetresFactice(), new ServiceCollection().BuildServiceProvider());
}
