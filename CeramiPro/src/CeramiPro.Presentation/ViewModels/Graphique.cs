using System.Collections.ObjectModel;
using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>Une barre de graphique, prête à dessiner.</summary>
/// <param name="Etiquette">Jour, mois ou catégorie représenté.</param>
/// <param name="EtiquetteAffichee">Texte sous la barre ; vide quand il chargerait l'axe.</param>
/// <param name="InfoBulle">Valeur exacte, montrée au survol.</param>
/// <param name="Hauteur">Hauteur de la barre, en pixels.</param>
public record BarreGraphique(string Etiquette, string EtiquetteAffichee, string InfoBulle, double Hauteur);

/// <summary>
/// Transforme une série de valeurs en barres. Une seule teinte, des barres
/// proportionnelles à la plus haute, une info-bulle sur chacune : c'est tout
/// ce qu'il faut pour lire une évolution, sans bibliothèque de graphiques.
/// </summary>
public static class Graphique
{
    /// <summary>Hauteur de la barre la plus haute, en pixels.</summary>
    public const double HauteurMaximale = 150;

    /// <summary>
    /// Remplit les barres et renvoie la plus haute valeur, mise en forme.
    /// Les valeurs négatives — un résultat en perte — sont dessinées à zéro,
    /// l'info-bulle et le tableau donnant le chiffre exact.
    /// </summary>
    public static string Remplir(
        ObservableCollection<BarreGraphique> barres, IReadOnlyList<PointGraphiqueDto> points,
        int etiquetteTousLes = 1)
    {
        barres.Clear();

        var maximum = points.Count == 0 ? 0m : points.Max(p => p.Valeur);
        var pas = Math.Max(1, etiquetteTousLes);

        for (var rang = 0; rang < points.Count; rang++)
        {
            var point = points[rang];
            var proportion = maximum <= 0 ? 0d : (double)(Math.Max(0m, point.Valeur) / maximum);

            // L'étiquette de la dernière barre — aujourd'hui, ce mois-ci — est
            // toujours affichée : c'est elle que l'on cherche du regard.
            var visible = (points.Count - 1 - rang) % pas == 0;

            barres.Add(new BarreGraphique(
                point.Etiquette,
                visible ? point.Etiquette : string.Empty,
                $"{point.Etiquette} : {Formatage.Montant(point.Valeur)}",
                Math.Round(proportion * HauteurMaximale, 1)));
        }

        return maximum > 0 ? Formatage.Montant(maximum) : "aucune vente sur la période";
    }

    /// <summary>Une étiquette toutes les N barres, pour que l'axe reste lisible.</summary>
    public static int PasDesEtiquettes(int nombreDeBarres) => nombreDeBarres switch
    {
        <= 14 => 1,
        <= 31 => 5,
        _ => 10
    };
}
