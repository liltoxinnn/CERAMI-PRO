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
            .Select(_ => (object?)null)
            .ToArray();

        return (IFormulaire)Activator.CreateInstance(type, parametres)!;
    }

    private static IReadOnlyList<ChampFormulaire> Champs(IFormulaire formulaire)
        => (IReadOnlyList<ChampFormulaire>)formulaire.GetType()
            .GetProperty("Champs")!.GetValue(formulaire)!;

    [Fact]
    public void Tous_les_formulaires_sont_declares()
        => TypesFormulaires().Should().HaveCount(10);

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
        var vue = new ClientFormulaireVueModele(null!);

        vue.Champs.Should().NotBeEmpty();
        vue.Champs.Should().Contain(c => c.Propriete == "Nom" && c.Obligatoire);
        vue.Titre.Should().Be("Nouveau client");
        vue.EstCreation.Should().BeTrue();
    }

    [Fact]
    public async Task Un_champ_obligatoire_vide_est_nomme_dans_le_message()
    {
        var vue = new ClientFormulaireVueModele(null!);

        await vue.ValiderCommand.ExecuteAsync(null);

        // Nommer le champ évite à l'utilisateur de chercher lequel manque.
        vue.MessageErreur.Should().Contain("Nom");
        vue.Enregistre.Should().BeFalse();
    }

    [Fact]
    public void Les_boutons_du_formulaire_sont_en_francais()
    {
        var vue = new ClientFormulaireVueModele(null!);

        vue.LibelleEnregistrer.Should().Be("Enregistrer");
        vue.LibelleAnnuler.Should().Be("Annuler");
    }

    [Fact]
    public async Task Le_role_employe_n_est_pas_pris_pour_un_champ_vide()
    {
        // « Employé » vaut zéro : il ne doit pas être confondu avec un choix
        // oublié, sans quoi aucun compte d'employé ne pourrait être créé.
        using var atelier = new Aides.AtelierDeTest();
        var vue = new UtilisateurFormulaireVueModele(new CeramiPro.Application.Services.UtilisateurService(
            atelier.Contexte, new CeramiPro.Infrastructure.Authentication.PasswordHasherService(),
            atelier.UtilisateurCourant, atelier.Audit, atelier.Horloge));

        vue.Requete.NomUtilisateur = "sara";
        vue.Requete.NomComplet = "Sara Mansouri";
        vue.Requete.MotDePasse = "Atelier@2026";

        await vue.ValiderCommand.ExecuteAsync(null);

        vue.MessageErreur.Should().BeNull();
        vue.Enregistre.Should().BeTrue();
        atelier.Contexte.Users.Single(u => u.UserName == "sara").Role
            .Should().Be(CeramiPro.Domain.Enums.RoleUtilisateur.Employe);
    }

    [Fact]
    public void Le_mot_de_passe_n_est_demande_qu_a_la_creation_d_un_compte()
    {
        var vue = new UtilisateurFormulaireVueModele(null!);

        vue.Champs.Should().Contain(c => c.Propriete == nameof(CompteRequete.MotDePasse));
    }
}

/// <summary>
/// Le changement de mot de passe proposé à la connexion.
///
/// Il est proposé, jamais imposé : empêcher quelqu'un d'ouvrir l'atelier
/// tant qu'il n'a pas changé son mot de passe coûterait plus cher que le
/// risque évité.
/// </summary>
public class ChangementMotDePasseTests
{
    private static ChangementMotDePasseVueModele Construire(bool auDemarrage)
        => new(null!) { ProposeAuDemarrage = auDemarrage };

    [Fact]
    public void La_proposition_du_demarrage_dit_qu_elle_peut_attendre()
    {
        var vue = Construire(auDemarrage: true);

        vue.Introduction.Should().Contain("plus tard");
        vue.LibelleAnnuler.Should().Be("Plus tard",
            "« Annuler » laisserait croire qu'on renonce à quelque chose");
    }

    [Fact]
    public void Le_changement_demande_par_l_utilisateur_garde_le_libelle_habituel()
    {
        var vue = Construire(auDemarrage: false);

        vue.LibelleAnnuler.Should().Be("Annuler");
        vue.Introduction.Should().NotContain("provisoire");
    }

    [Fact]
    public async Task Les_deux_saisies_du_nouveau_mot_de_passe_doivent_correspondre()
    {
        var vue = Construire(auDemarrage: true);
        vue.MotDePasseActuel = "Ancien@2026";
        vue.NouveauMotDePasse = "Nouveau@2026";
        vue.Confirmation = "Nouveau@2027";

        await vue.ValiderCommand.ExecuteAsync(null);

        vue.MessageErreur.Should().Contain("ne correspondent pas");
        vue.Change.Should().BeFalse();
    }

    [Fact]
    public async Task Le_nouveau_mot_de_passe_doit_differer_de_l_ancien()
    {
        var vue = Construire(auDemarrage: true);
        vue.MotDePasseActuel = "Pareil@2026";
        vue.NouveauMotDePasse = "Pareil@2026";
        vue.Confirmation = "Pareil@2026";

        await vue.ValiderCommand.ExecuteAsync(null);

        vue.MessageErreur.Should().Contain("différent");
        vue.Change.Should().BeFalse();
    }

    [Fact]
    public async Task Une_saisie_incomplete_nomme_ce_qui_manque()
    {
        var vue = Construire(auDemarrage: true);

        await vue.ValiderCommand.ExecuteAsync(null);

        vue.MessageErreur.Should().Contain("actuel");
    }
}
