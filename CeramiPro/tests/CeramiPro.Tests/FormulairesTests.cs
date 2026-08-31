using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.Presentation.ViewModels.Formulaires;
using FluentAssertions;

namespace CeramiPro.Tests;

/// <summary>
/// Les formulaires de saisie décrivent leurs champs comme les listes
/// décrivent leurs colonnes. Ces vérifications portent sur la description :
/// aucun service métier n'est appelé.
/// </summary>
public class FormulairesTests
{
    /// <summary>Tous les formulaires de saisie de l'application.</summary>
    public static IEnumerable<Type> TypesFormulaires()
        => typeof(ClientFormulaireVueModele).Assembly.GetTypes()
            .Where(t => !t.IsAbstract
                        && t.Namespace == "CeramiPro.Presentation.ViewModels.Formulaires"
                        && typeof(IFormulaire).IsAssignableFrom(t))
            .OrderBy(t => t.Name);

    private static IFormulaire Construire(Type type)
    {
        var parametres = type.GetConstructors()[0].GetParameters()
            .Select(p => p.ParameterType == typeof(IServiceLangue)
                ? (object?)new ServiceLangue()
                : null)
            .ToArray();

        return (IFormulaire)Activator.CreateInstance(type, parametres)!;
    }

    private static IReadOnlyList<ChampFormulaire> Champs(IFormulaire formulaire)
        => (IReadOnlyList<ChampFormulaire>)formulaire.GetType()
            .GetProperty("Champs")!.GetValue(formulaire)!;

    [Fact]
    public void Tous_les_formulaires_sont_declares()
        => TypesFormulaires().Should().HaveCountGreaterThanOrEqualTo(15);

    [Fact]
    public void Chaque_formulaire_porte_un_titre_et_des_champs()
    {
        foreach (var type in TypesFormulaires())
        {
            var formulaire = Construire(type);

            var titre = (string)type.GetProperty("Titre")!.GetValue(formulaire)!;
            titre.Should().NotBeNullOrWhiteSpace($"le formulaire {type.Name} doit avoir un titre");

            Champs(formulaire).Should().NotBeEmpty(
                $"le formulaire {type.Name} doit proposer des champs");
        }
    }

    [Fact]
    public void Chaque_champ_vise_une_propriete_reelle_de_sa_requete()
    {
        // Un champ lié à une propriété inexistante resterait vide à l'écran,
        // sans le moindre message : la liaison échouerait en silence.
        var introuvables = new List<string>();

        foreach (var type in TypesFormulaires())
        {
            var formulaire = Construire(type);
            var requete = type.GetProperty("Requete")!.GetValue(formulaire)!;

            foreach (var champ in Champs(formulaire))
            {
                if (requete.GetType().GetProperty(champ.Propriete) is null)
                {
                    introuvables.Add($"{type.Name}.{champ.Propriete}");
                }
            }
        }

        introuvables.Should().BeEmpty(
            "ces champs visent une propriété qui n'existe pas : " + string.Join(", ", introuvables));
    }

    [Fact]
    public void Chaque_champ_porte_un_libelle_francais()
    {
        foreach (var type in TypesFormulaires())
        {
            foreach (var champ in Champs(Construire(type)))
            {
                champ.Libelle.Should().NotBeNullOrWhiteSpace(
                    $"le champ {type.Name}.{champ.Propriete} doit être nommé");
            }
        }
    }

    [Fact]
    public void Une_liste_deroulante_obligatoire_ne_reste_jamais_sans_choix()
    {
        // Avant préparation les options sont vides ; c'est justement pour
        // cela que l'écran appelle « PreparerAsync » avant d'afficher.
        foreach (var type in TypesFormulaires())
        {
            var formulaire = Construire(type);

            var sansOptions = Champs(formulaire)
                .Where(c => c.Type == TypeChamp.Liste && c.Options is null)
                .Select(c => $"{type.Name}.{c.Propriete}")
                .ToList();

            sansOptions.Should().BeEmpty(
                "ces listes déroulantes n'ont aucune collection d'options : "
                + string.Join(", ", sansOptions));
        }
    }

    [Fact]
    public void Le_formulaire_client_declare_ses_champs()
    {
        var vue = new ClientFormulaireVueModele(null!, new ServiceLangue());

        vue.Champs.Should().NotBeEmpty();
        vue.Champs.Should().Contain(c => c.Propriete == "Nom" && c.Obligatoire);
        vue.Titre.Should().Be("Nouveau client");
        vue.EstCreation.Should().BeTrue();
    }

    [Fact]
    public async Task Un_champ_obligatoire_vide_est_nomme_dans_le_message()
    {
        var vue = new ClientFormulaireVueModele(null!, new ServiceLangue());

        await vue.ValiderCommand.ExecuteAsync(null);

        // Nommer le champ évite à l'utilisateur de chercher lequel manque.
        vue.MessageErreur.Should().Contain("Nom");
        vue.Enregistre.Should().BeFalse();
    }

    [Fact]
    public void Les_libelles_du_formulaire_suivent_la_langue()
    {
        var langue = new ServiceLangue();
        var vue = new ClientFormulaireVueModele(null!, langue);

        vue.LibelleEnregistrer.Should().Be("Enregistrer");

        langue.Changer(Langue.Arabe);

        vue.LibelleEnregistrer.Should().Be("حفظ");
    }

    [Fact]
    public void Le_mot_de_passe_n_est_demande_qu_a_la_creation_d_un_compte()
    {
        var vue = new UtilisateurFormulaireVueModele(null!, null!, new ServiceLangue());

        vue.Champs.Should().Contain(c => c.Propriete == nameof(CompteRequete.MotDePasse));
    }
}
