using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.App.Services;

/// <summary>
/// Boîtes de dialogue « Enregistrer sous » de Windows, et ouverture d'un
/// fichier avec le programme que Windows lui associe.
/// </summary>
public class ServiceFichier : IServiceFichier
{
    /// <summary>
    /// Dossier proposé par défaut : « Documents\CeramiPro ». Un dossier
    /// nommé évite à l'utilisateur de retrouver ses exports éparpillés.
    /// </summary>
    public static string DossierPropose { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CeramiPro");

    public string? DemanderOuEnregistrer(string nomPropose, string filtre)
    {
        Directory.CreateDirectory(DossierPropose);

        var boite = new SaveFileDialog
        {
            FileName = nomPropose,
            Filter = filtre,
            InitialDirectory = DossierPropose,
            AddExtension = true,
            OverwritePrompt = true,
            Title = "CeramiPro — Enregistrer sous"
        };

        return boite.ShowDialog() == true ? boite.FileName : null;
    }

    public void Ouvrir(string chemin)
    {
        // Un export que l'on ne peut pas relire n'a servi à rien : si Windows
        // n'a aucun programme associé, on ouvre le dossier qui le contient.
        try
        {
            Process.Start(new ProcessStartInfo(chemin) { UseShellExecute = true });
        }
        catch (Exception)
        {
            var dossier = Path.GetDirectoryName(chemin);

            if (!string.IsNullOrEmpty(dossier) && Directory.Exists(dossier))
            {
                Process.Start(new ProcessStartInfo(dossier) { UseShellExecute = true });
            }
        }
    }
}
