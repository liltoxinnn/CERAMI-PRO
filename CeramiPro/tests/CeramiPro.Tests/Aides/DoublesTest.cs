using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Tests.Aides;

/// <summary>Session simulée : on choisit l'utilisateur et son rôle.</summary>
public class UtilisateurCourantFactice : ISessionAtelier
{
    public int? UtilisateurId { get; set; } = 1;

    public string? NomUtilisateur { get; set; } = "admin";

    public string? NomComplet { get; set; } = "Administrateur de l'atelier";

    public RoleUtilisateur? Role { get; set; } = RoleUtilisateur.Administrateur;

    public string? NomRole => Role?.Libelle();

    public bool EstConnecte => UtilisateurId is not null;

    public bool EstAdministrateur => Role == RoleUtilisateur.Administrateur;

    public void Ouvrir(int utilisateurId, string nomUtilisateur, string nomComplet, RoleUtilisateur role)
    {
        UtilisateurId = utilisateurId;
        NomUtilisateur = nomUtilisateur;
        NomComplet = nomComplet;
        Role = role;
    }

    public void Fermer()
    {
        UtilisateurId = null;
        NomUtilisateur = null;
        NomComplet = null;
        Role = null;
    }
}

/// <summary>Nom court employé par les tests d'interface.</summary>
public class UtilisateurFactice : UtilisateurCourantFactice;

/// <summary>Horloge fixe : les tests ne dépendent pas de l'heure réelle.</summary>
public class HorlogeFactice : IServiceDateHeure
{
    public HorlogeFactice(DateTime? depart = null)
        => MaintenantUtc = depart ?? new DateTime(2026, 8, 31, 9, 0, 0, DateTimeKind.Utc);

    public DateTime MaintenantUtc { get; set; }

    public DateTime MaintenantAtelier => MaintenantUtc.AddHours(1);

    public DateTime Aujourdhui => MaintenantAtelier.Date;

    public DateTime VersHeureAtelier(DateTime utc) => utc.AddHours(1);

    public DateTime VersUtc(DateTime heureAtelier) => heureAtelier.AddHours(-1);

    public void Avancer(TimeSpan duree) => MaintenantUtc = MaintenantUtc.Add(duree);
}

/// <summary>Journal d'audit simulé : conserve les opérations enregistrées.</summary>
public class AuditFactice : IAuditService
{
    public List<(AuditAction Action, string Entite, string? Identifiant, string? Description)> Traces { get; } = new();

    public Task EnregistrerAsync(
        AuditAction action,
        string nomEntite,
        string? identifiantEntite = null,
        string? description = null,
        object? changements = null,
        CancellationToken cancellationToken = default)
    {
        Traces.Add((action, nomEntite, identifiantEntite, description));
        return Task.CompletedTask;
    }
}

/// <summary>Dialogues simulés : on vérifie ce qui aurait été affiché.</summary>
public class DialogueFactice : IServiceDialogue
{
    public List<(string Niveau, string Message)> Messages { get; } = new();

    public bool ReponseConfirmation { get; set; } = true;

    public void Information(string message, string titre = "Information")
        => Messages.Add(("information", message));

    public void Succes(string message, string titre = "Opération réussie")
        => Messages.Add(("succes", message));

    public void Avertissement(string message, string titre = "Attention")
        => Messages.Add(("avertissement", message));

    public void Erreur(string message, string titre = "Erreur")
        => Messages.Add(("erreur", message));

    public bool Confirmer(string message, string titre = "Confirmation")
    {
        Messages.Add(("confirmation", message));
        return ReponseConfirmation;
    }

    /// <summary>Texte « saisi » par l'utilisateur ; nul pour représenter un renoncement.</summary>
    public string? ReponseDemande { get; set; } = "Motif saisi pendant le test";

    public string? Demander(string message, string titre = "Précision", string valeurInitiale = "",
        bool obligatoire = true)
    {
        Messages.Add(("question", message));
        return ReponseDemande;
    }
}

/// <summary>Base de données simulée : on choisit si elle répond ou non.</summary>
public class EtatBaseFactice : IServiceEtatBaseDeDonnees
{
    public bool Disponible { get; set; } = true;

    public Task<EtatBaseDeDonnees> VerifierAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Disponible
            ? EtatBaseDeDonnees.Connectee("CeramiProDB")
            : EtatBaseDeDonnees.Injoignable());

    public Task<InformationsBaseDeDonnees> DecrireAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new InformationsBaseDeDonnees(
            Disponible, "localhost", 5432, "CeramiProDB", "postgres", "16.4",
            "Pieces uniques et canaux de commande (06/11/2026)", 0, 0, "appsettings.Local.json"));
}

/// <summary>
/// Fenêtre de saisie simulée : on choisit si l'utilisateur enregistre ou
/// renonce, et l'on retient les formulaires qui ont été affichés.
///
/// Avec <see cref="PendantLaSaisie"/>, le test remplit le formulaire puis
/// la fenêtre appuie sur « Enregistrer », comme le ferait l'utilisateur :
/// la saisie passe alors réellement par le service métier.
/// </summary>
public class FormulaireFactice : IServiceFormulaire
{
    public List<object> Affiches { get; } = new();

    public bool Enregistre { get; set; } = true;

    /// <summary>Saisie de l'utilisateur, jouée avant d'appuyer sur « Enregistrer ».</summary>
    public Action<object>? PendantLaSaisie { get; set; }

    /// <summary>Message affiché dans le formulaire après « Enregistrer », s'il y en a un.</summary>
    public string? DerniereErreur { get; private set; }

    public bool Afficher(object vueModeleFormulaire)
    {
        Affiches.Add(vueModeleFormulaire);

        if (PendantLaSaisie is null)
        {
            return Enregistre;
        }

        PendantLaSaisie(vueModeleFormulaire);

        if (!Enregistre)
        {
            return false;
        }

        var type = vueModeleFormulaire.GetType();
        var valider = (CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)type
            .GetProperty("ValiderCommand")!.GetValue(vueModeleFormulaire)!;

        valider.ExecuteAsync(null).GetAwaiter().GetResult();

        DerniereErreur = type.GetProperty("MessageErreur")?.GetValue(vueModeleFormulaire) as string;
        return type.GetProperty("Enregistre")?.GetValue(vueModeleFormulaire) as bool? ?? false;
    }
}

/// <summary>
/// Boîte « Enregistrer sous » simulée : on choisit le chemin renvoyé, ou
/// <c>null</c> pour représenter un utilisateur qui renonce.
/// </summary>
public class FichierFactice : IServiceFichier
{
    public string? CheminChoisi { get; set; }

    public List<string> Demandes { get; } = new();

    public List<string> Ouverts { get; } = new();

    public string? DemanderOuEnregistrer(string nomPropose, string filtre)
    {
        Demandes.Add(nomPropose);
        return CheminChoisi;
    }

    public void Ouvrir(string chemin) => Ouverts.Add(chemin);

    /// <summary>Fichiers « choisis » dans la boîte « Ouvrir ».</summary>
    public List<string> FichiersChoisis { get; } = new();

    public IReadOnlyList<string> Choisir(string titre, string filtre, bool plusieurs = true)
        => FichiersChoisis.ToList();
}

/// <summary>Fenêtres de consultation simulées : on retient celles qui ont été ouvertes.</summary>
public class FenetresFactice : IServiceFenetres
{
    public List<Presentation.ViewModels.VueModeleBase> Ouvertes { get; } = new();

    /// <summary>Action jouée « dans » la fenêtre, avant sa fermeture.</summary>
    public Func<Presentation.ViewModels.VueModeleBase, Task>? PendantLaFenetre { get; set; }

    public void Afficher(Presentation.ViewModels.VueModeleBase vueModele)
    {
        Ouvertes.Add(vueModele);
        vueModele.ChargerAsync().GetAwaiter().GetResult();
        PendantLaFenetre?.Invoke(vueModele).GetAwaiter().GetResult();
    }
}
