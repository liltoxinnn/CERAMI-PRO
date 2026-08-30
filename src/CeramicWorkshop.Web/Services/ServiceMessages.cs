namespace CeramicWorkshop.Web.Services;

/// <summary>Niveau d'un message affiché à l'utilisateur.</summary>
public enum NiveauMessage
{
    Succes,
    Information,
    Avertissement,
    Erreur
}

/// <summary>Message temporaire affiché en haut de l'écran.</summary>
public record MessageUtilisateur(Guid Id, NiveauMessage Niveau, string Texte);

/// <summary>
/// Affiche les confirmations et les erreurs sous forme de bandeaux temporaires,
/// pour que l'utilisateur sache toujours si son action a été enregistrée.
/// </summary>
public class ServiceMessages
{
    private readonly List<MessageUtilisateur> _messages = new();

    public IReadOnlyList<MessageUtilisateur> Messages => _messages;

    public event Action? Modifie;

    public void Succes(string texte) => Ajouter(NiveauMessage.Succes, texte);

    public void Information(string texte) => Ajouter(NiveauMessage.Information, texte);

    public void Avertissement(string texte) => Ajouter(NiveauMessage.Avertissement, texte);

    public void Erreur(string texte) => Ajouter(NiveauMessage.Erreur, texte);

    public void Fermer(Guid id)
    {
        _messages.RemoveAll(m => m.Id == id);
        Modifie?.Invoke();
    }

    private void Ajouter(NiveauMessage niveau, string texte)
    {
        if (string.IsNullOrWhiteSpace(texte))
        {
            return;
        }

        _messages.Add(new MessageUtilisateur(Guid.NewGuid(), niveau, texte));
        Modifie?.Invoke();
    }
}
