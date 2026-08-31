using CeramiPro.Application.Common;

namespace CeramiPro.Application.Localisation;

/// <summary>
/// Langue de l'interface.
///
/// Le français sert de secours : une clé absente de l'arabe est affichée en
/// français plutôt que sous forme technique. Une clé absente des deux
/// dictionnaires renvoie la clé elle-même, ce qui rend l'oubli visible sans
/// faire échouer l'écran.
/// </summary>
public class ServiceLangue : IServiceLangue
{
    private IReadOnlyDictionary<string, string> _traductions = Traductions.Francais;

    public Langue LangueCourante { get; private set; } = Langue.Francais;

    public SensEcriture Sens => LangueCourante.Sens();

    public event Action? LangueChangee;

    public string this[string cle] => Traduire(cle);

    public string Traduire(string cle, params object[] valeurs)
    {
        var texte = Texte(cle);

        return valeurs.Length == 0
            ? texte
            : string.Format(ParametresAtelier.Culture, texte, valeurs);
    }

    public void Changer(Langue langue)
    {
        if (langue == LangueCourante)
        {
            return;
        }

        LangueCourante = langue;
        _traductions = Traductions.Pour(langue);

        LangueChangee?.Invoke();
    }

    private string Texte(string cle)
    {
        if (string.IsNullOrWhiteSpace(cle))
        {
            return string.Empty;
        }

        if (_traductions.TryGetValue(cle, out var traduction))
        {
            return traduction;
        }

        // Repli sur le français : mieux vaut un texte compréhensible par le
        // patron de l'atelier qu'une clé technique affichée à l'écran.
        return Traductions.Francais.TryGetValue(cle, out var francais) ? francais : cle;
    }
}
