using System.Globalization;
using System.Text;

namespace CeramicWorkshop.Application.Common;

/// <summary>
/// Comparaison souple de deux textes. Elle sert à la recherche globale : le
/// nom d'un client mal orthographié, saisi sans accent ou avec une lettre en
/// trop, doit tout de même être retrouvé.
/// </summary>
public static class Similitude
{
    /// <summary>Score minimal, sur 100, pour considérer qu'un texte correspond.</summary>
    public const int SeuilPertinence = 60;

    /// <summary>
    /// Met un texte à plat : minuscules, sans accent et sans espaces superflus.
    /// « Émaillé » et « emaille » deviennent ainsi comparables.
    /// </summary>
    public static string Aplatir(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte))
        {
            return string.Empty;
        }

        var decompose = texte.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var resultat = new StringBuilder(decompose.Length);

        foreach (var caractere in decompose)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            {
                resultat.Append(caractere);
            }
        }

        return resultat.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Note de 0 à 100 la ressemblance entre le texte cherché et un candidat.
    /// Une correspondance exacte vaut 100, un début de mot 90, une inclusion 80,
    /// et au-delà on tolère les fautes de frappe.
    /// </summary>
    public static int Noter(string? recherche, string? candidat)
    {
        var terme = Aplatir(recherche);
        var valeur = Aplatir(candidat);

        if (terme.Length == 0 || valeur.Length == 0)
        {
            return 0;
        }

        if (valeur == terme)
        {
            return 100;
        }

        if (valeur.StartsWith(terme, StringComparison.Ordinal))
        {
            return 90;
        }

        if (valeur.Contains(terme, StringComparison.Ordinal))
        {
            return 80;
        }

        // Un mot du candidat commence-t-il par le terme cherché ?
        foreach (var mot in valeur.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (mot.StartsWith(terme, StringComparison.Ordinal))
            {
                return 85;
            }
        }

        // Sinon on tolère quelques fautes de frappe. La comparaison porte sur le
        // texte entier et sur chacun de ses mots : « benalli » doit retrouver
        // « Mohamed Benali », où seul le second mot ressemble au terme cherché.
        var distance = DistanceLaPlusProche(terme, valeur);
        var longueur = Math.Max(terme.Length, 1);
        var note = (int)Math.Round(100m * (1m - (decimal)distance / longueur));

        return Math.Clamp(note, 0, 75);
    }

    /// <summary>Plus petite distance entre le terme et le texte ou l'un de ses mots.</summary>
    private static int DistanceLaPlusProche(string terme, string valeur)
    {
        var distance = Distance(terme, valeur);

        foreach (var mot in valeur.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            distance = Math.Min(distance, Distance(terme, mot));
        }

        return distance;
    }

    /// <summary>Distance de Levenshtein : nombre de corrections d'une chaîne à l'autre.</summary>
    public static int Distance(string gauche, string droite)
    {
        if (gauche.Length == 0) return droite.Length;
        if (droite.Length == 0) return gauche.Length;

        var precedente = new int[droite.Length + 1];
        var courante = new int[droite.Length + 1];

        for (var colonne = 0; colonne <= droite.Length; colonne++)
        {
            precedente[colonne] = colonne;
        }

        for (var ligne = 1; ligne <= gauche.Length; ligne++)
        {
            courante[0] = ligne;

            for (var colonne = 1; colonne <= droite.Length; colonne++)
            {
                var cout = gauche[ligne - 1] == droite[colonne - 1] ? 0 : 1;

                courante[colonne] = Math.Min(
                    Math.Min(courante[colonne - 1] + 1, precedente[colonne] + 1),
                    precedente[colonne - 1] + cout);
            }

            (precedente, courante) = (courante, precedente);
        }

        return precedente[droite.Length];
    }
}
