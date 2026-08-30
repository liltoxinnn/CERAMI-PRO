using System.Globalization;
using System.Text;
using CeramicWorkshop.Application.Interfaces;
using QRCoder;

namespace CeramicWorkshop.Infrastructure.Services;

/// <summary>
/// Fabrique les images des codes imprimés sur les étiquettes.
///
/// Le code QR est produit par la bibliothèque QRCoder ; le code-barres est un
/// Code 39, format reconnu par toutes les douchettes USB du commerce et assez
/// simple pour être dessiné directement en SVG.
/// </summary>
public class CodeGraphiqueService : ICodeGraphiqueService
{
    /// <summary>Largeur d'une barre fine, en pixels.</summary>
    private const int BarreFine = 2;

    /// <summary>Une barre large vaut trois barres fines (rapport recommandé).</summary>
    private const int BarreLarge = BarreFine * 3;

    /// <summary>Marge blanche obligatoire de part et d'autre du code-barres.</summary>
    private const int MargeSilence = BarreFine * 10;

    /// <summary>
    /// Table du Code 39 : neuf éléments par caractère, en alternant barre et
    /// espace. « e » désigne un élément fin, « L » un élément large.
    /// </summary>
    private static readonly Dictionary<char, string> Motifs = new()
    {
        ['0'] = "eeeLLeLee", ['1'] = "LeeLeeeeL", ['2'] = "eeLLeeeeL", ['3'] = "LeLLeeeee",
        ['4'] = "eeeLLeeeL", ['5'] = "LeeLLeeee", ['6'] = "eeLLLeeee", ['7'] = "eeeLeeLeL",
        ['8'] = "LeeLeeLee", ['9'] = "eeLLeeLee",
        ['A'] = "LeeeeLeeL", ['B'] = "eeLeeLeeL", ['C'] = "LeLeeLeee", ['D'] = "eeeeLLeeL",
        ['E'] = "LeeeLLeee", ['F'] = "eeLeLLeee", ['G'] = "eeeeeLLeL", ['H'] = "LeeeeLLee",
        ['I'] = "eeLeeLLee", ['J'] = "eeeeLLLee", ['K'] = "LeeeeeeLL", ['L'] = "eeLeeeeLL",
        ['M'] = "LeLeeeeLe", ['N'] = "eeeeLeeLL", ['O'] = "LeeeLeeLe", ['P'] = "eeLeLeeLe",
        ['Q'] = "eeeeeeLLL", ['R'] = "LeeeeeLLe", ['S'] = "eeLeeeLLe", ['T'] = "eeeeLeLLe",
        ['U'] = "LLeeeeeeL", ['V'] = "eLLeeeeeL", ['W'] = "LLLeeeeee", ['X'] = "eLeeLeeeL",
        ['Y'] = "LLeeLeeee", ['Z'] = "eLLeLeeee",
        ['-'] = "eLeeeeLeL", ['.'] = "LLeeeeLee", [' '] = "eLLeeeLee", ['$'] = "eLeLeLeee",
        ['/'] = "eLeLeeeLe", ['+'] = "eLeeeLeLe", ['%'] = "eeeLeLeLe", ['*'] = "eLeeLeLee"
    };

    public string QrEnSvg(string valeur, int tailleEnPixels = 160)
    {
        var contenu = string.IsNullOrWhiteSpace(valeur) ? " " : valeur.Trim();

        using var generateur = new QRCodeGenerator();
        using var donnees = generateur.CreateQrCode(contenu, QRCodeGenerator.ECCLevel.M);

        var modules = donnees.ModuleMatrix.Count;
        var svg = new StringBuilder();

        svg.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {modules} {modules}\" ")
           .Append(CultureInfo.InvariantCulture,
               $"width=\"{tailleEnPixels}\" height=\"{tailleEnPixels}\" role=\"img\" ")
           .Append(CultureInfo.InvariantCulture, $"aria-label=\"Code QR {Echapper(contenu)}\">")
           .Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>")
           .Append("<path fill=\"#000000\" d=\"");

        for (var ligne = 0; ligne < modules; ligne++)
        {
            var colonnes = donnees.ModuleMatrix[ligne];

            for (var colonne = 0; colonne < modules; colonne++)
            {
                if (colonnes[colonne])
                {
                    svg.Append(CultureInfo.InvariantCulture, $"M{colonne} {ligne}h1v1h-1z");
                }
            }
        }

        svg.Append("\"/></svg>");
        return svg.ToString();
    }

    public string CodeBarresEnSvg(string valeur, int hauteurEnPixels = 60)
    {
        var contenu = Normaliser(valeur);

        if (contenu.Length == 0 || !EstImprimableEnCodeBarres(contenu))
        {
            return string.Empty;
        }

        // Le Code 39 encadre toujours la valeur par le caractère « * ».
        var caracteres = $"*{contenu}*";
        var barres = new List<(int Largeur, bool Noire)>();

        foreach (var caractere in caracteres)
        {
            var motif = Motifs[caractere];

            for (var element = 0; element < motif.Length; element++)
            {
                barres.Add((motif[element] == 'L' ? BarreLarge : BarreFine, element % 2 == 0));
            }

            // Espace fin de séparation entre deux caractères.
            barres.Add((BarreFine, false));
        }

        var largeur = barres.Sum(b => b.Largeur) + MargeSilence * 2;
        var hauteurTotale = hauteurEnPixels + 18;

        var svg = new StringBuilder();

        svg.Append(CultureInfo.InvariantCulture,
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {largeur} {hauteurTotale}\" ")
           .Append(CultureInfo.InvariantCulture,
               $"width=\"{largeur}\" height=\"{hauteurTotale}\" role=\"img\" ")
           .Append(CultureInfo.InvariantCulture, $"aria-label=\"Code-barres {Echapper(contenu)}\">")
           .Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");

        var position = MargeSilence;

        foreach (var (largeurBarre, noire) in barres)
        {
            if (noire)
            {
                svg.Append(CultureInfo.InvariantCulture,
                    $"<rect x=\"{position}\" y=\"0\" width=\"{largeurBarre}\" " +
                    $"height=\"{hauteurEnPixels}\" fill=\"#000000\"/>");
            }

            position += largeurBarre;
        }

        svg.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{largeur / 2}\" y=\"{hauteurTotale - 4}\" text-anchor=\"middle\" ")
           .Append("font-family=\"monospace\" font-size=\"13\" fill=\"#000000\">")
           .Append(Echapper(contenu))
           .Append("</text></svg>");

        return svg.ToString();
    }

    public bool EstImprimableEnCodeBarres(string valeur)
    {
        var contenu = Normaliser(valeur);

        return contenu.Length > 0 && contenu.All(Motifs.ContainsKey) && !contenu.Contains('*');
    }

    /// <summary>Le Code 39 ne connaît que les majuscules et quelques symboles.</summary>
    private static string Normaliser(string? valeur)
        => (valeur ?? string.Empty).Trim().ToUpperInvariant();

    private static string Echapper(string valeur)
        => valeur.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                 .Replace("\"", "&quot;");
}
