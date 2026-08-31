namespace CeramiPro.Application.Localisation;

/// <summary>
/// Langue de l'interface. Le changement est immédiat : les écrans se
/// remettent à jour sans redémarrer le logiciel.
/// </summary>
public interface IServiceLangue
{
    Langue LangueCourante { get; }

    SensEcriture Sens { get; }

    /// <summary>Traduit une clé. Renvoie la clé elle-même si la traduction manque.</summary>
    string this[string cle] { get; }

    /// <summary>Traduit une clé en insérant des valeurs : « Il reste {0} pièces ».</summary>
    string Traduire(string cle, params object[] valeurs);

    void Changer(Langue langue);

    event Action? LangueChangee;
}
