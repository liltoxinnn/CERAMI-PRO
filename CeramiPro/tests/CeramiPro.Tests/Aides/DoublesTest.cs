using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Tests.Aides;

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
}

/// <summary>Utilisateur simulé, dont on choisit les droits.</summary>
public class UtilisateurFactice : IUtilisateurCourant
{
    public int? UtilisateurId { get; set; } = 1;

    public string? NomUtilisateur { get; set; } = "admin";

    public string? CodeRole { get; set; } = "administrateur";

    public bool EstConnecte => UtilisateurId is not null;

    public HashSet<string> Droits { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool PossedeDroit(string codeDroit) => Droits.Contains(codeDroit);
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
public class EtatBaseFactice : CeramiPro.Application.Interfaces.IServiceEtatBaseDeDonnees
{
    public bool Disponible { get; set; } = true;

    public Task<CeramiPro.Application.Interfaces.EtatBaseDeDonnees> VerifierAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult(Disponible
            ? CeramiPro.Application.Interfaces.EtatBaseDeDonnees.Connectee("CeramiProDB")
            : CeramiPro.Application.Interfaces.EtatBaseDeDonnees.Injoignable());
}
