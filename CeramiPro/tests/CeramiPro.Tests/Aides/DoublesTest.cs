using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Tests.Aides;

/// <summary>Session simulée : on choisit l'utilisateur et ses droits.</summary>
public class UtilisateurCourantFactice : ISessionAtelier
{
    public int? UtilisateurId { get; set; } = 1;

    public string? NomUtilisateur { get; set; } = "admin";

    public string? NomComplet { get; set; } = "Administrateur de l'atelier";

    public string? CodeRole { get; set; } = "administrateur";

    public string? NomRole { get; set; } = "Administrateur";

    public bool EstConnecte => UtilisateurId is not null;

    public HashSet<string> Droits { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool PossedeDroit(string codeDroit) => Droits.Contains(codeDroit);

    public void Ouvrir(int utilisateurId, string nomUtilisateur, string nomComplet,
        string codeRole, string nomRole, IEnumerable<string> droits)
    {
        UtilisateurId = utilisateurId;
        NomUtilisateur = nomUtilisateur;
        NomComplet = nomComplet;
        CodeRole = codeRole;
        NomRole = nomRole;

        Droits.Clear();
        foreach (var droit in droits)
        {
            Droits.Add(droit);
        }
    }

    public void Fermer()
    {
        UtilisateurId = null;
        NomUtilisateur = null;
        NomComplet = null;
        CodeRole = null;
        NomRole = null;
        Droits.Clear();
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
}

/// <summary>Base de données simulée : on choisit si elle répond ou non.</summary>
public class EtatBaseFactice : IServiceEtatBaseDeDonnees
{
    public bool Disponible { get; set; } = true;

    public Task<EtatBaseDeDonnees> VerifierAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Disponible
            ? EtatBaseDeDonnees.Connectee("CeramiProDB")
            : EtatBaseDeDonnees.Injoignable());
}

/// <summary>
/// Fenêtre de saisie simulée : on choisit si l'utilisateur enregistre ou
/// renonce, et l'on retient les formulaires qui ont été affichés.
/// </summary>
public class FormulaireFactice : IServiceFormulaire
{
    public List<object> Affiches { get; } = new();

    public bool Enregistre { get; set; } = true;

    public bool Afficher(object vueModeleFormulaire)
    {
        Affiches.Add(vueModeleFormulaire);
        return Enregistre;
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
}
