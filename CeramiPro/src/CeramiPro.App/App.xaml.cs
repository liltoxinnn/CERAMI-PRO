using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using CeramiPro.Application;
using CeramiPro.Application.Common;
using CeramiPro.Application.Localisation;
using CeramiPro.Infrastructure;
using CeramiPro.Infrastructure.Data;
using CeramiPro.Presentation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.App.Services;
using CeramiPro.App.Vues;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CeramiPro.App;

/// <summary>
/// Point d'entrée de l'application Windows.
///
/// Au démarrage : lecture de la configuration, mise en place de la
/// journalisation, construction des services, vérification de la base de
/// données, puis ouverture de la fenêtre principale.
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _hote;

    /// <summary>Réglages livrés avec le logiciel.</summary>
    public const string FichierReglages = "appsettings.json";

    /// <summary>
    /// Réglages propres à l'ordinateur, à créer à l'installation. Il contient
    /// le mot de passe de la base : il n'est donc jamais versionné.
    /// </summary>
    public const string FichierReglagesLocaux = "appsettings.Local.json";

    /// <summary>
    /// Prévient si le fichier de réglages locaux existe mais n'est pas
    /// exploitable. Un fichier créé à la main et laissé vide est fréquent :
    /// mieux vaut un avertissement clair qu'un démarrage qui échoue.
    /// </summary>
    private static void VerifierReglagesLocaux()
    {
        foreach (var dossier in new[] { AppContext.BaseDirectory, DossierDonnees })
        {
            VerifierUnFichierDeReglages(Path.Combine(dossier, FichierReglagesLocaux));
        }
    }

    private static void VerifierUnFichierDeReglages(string chemin)
    {
        if (!File.Exists(chemin))
        {
            return;
        }

        try
        {
            var contenu = File.ReadAllText(chemin);

            if (string.IsNullOrWhiteSpace(contenu))
            {
                AvertirReglagesLocaux("il est vide", chemin);
                return;
            }

            System.Text.Json.JsonDocument.Parse(contenu).Dispose();
        }
        catch (Exception erreur)
        {
            AvertirReglagesLocaux($"il contient une erreur d'écriture ({erreur.Message})", chemin);
        }
    }

    private static void AvertirReglagesLocaux(string raison, string chemin)
        => MessageBox.Show(
            $"Le fichier de réglages « {FichierReglagesLocaux} » a été ignoré : {raison}.\n\n" +
            "Le logiciel va tenter de démarrer avec les réglages par défaut, " +
            "qui ne comportent pas de mot de passe de base de données.\n\n" +
            "Fichier concerné :\n" + chemin,
            "CeramiPro", MessageBoxButton.OK, MessageBoxImage.Warning);

    /// <summary>Dossier de travail : journaux, images, documents, sauvegardes.</summary>
    public static string DossierDonnees { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CeramiPro");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Toute l'interface parle français et affiche les nombres au format algérien.
        CultureInfo.DefaultThreadCurrentCulture = ParametresAtelier.Culture;
        CultureInfo.DefaultThreadCurrentUICulture = ParametresAtelier.Culture;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(ParametresAtelier.Culture.IetfLanguageTag)));

        Directory.CreateDirectory(DossierDonnees);

        // Une erreur imprévue ne doit jamais fermer le logiciel sans un mot
        // d'explication : le détail part au journal, l'utilisateur reçoit une
        // phrase en français et peut continuer son travail.
        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Erreur inattendue dans l'interface.");

            MessageBox.Show(
                "Une erreur est survenue pendant cette opération.\n\n" +
                "Vos données enregistrées ne sont pas affectées. Si le problème\n" +
                "se répète, le détail technique se trouve dans :\n" +
                Path.Combine(DossierDonnees, "journaux"),
                "CeramiPro", MessageBoxButton.OK, MessageBoxImage.Warning);

            args.Handled = true;
        };

        try
        {
            _hote = ConstruireHote();
            await _hote.StartAsync();

            if (!await BaseDeDonneesPreteAsync())
            {
                Shutdown(1);
                return;
            }

            if (!OuvrirSession())
            {
                Shutdown(0);
                return;
            }

            var fenetre = _hote.Services.GetRequiredService<FenetrePrincipale>();
            fenetre.DataContext = _hote.Services.GetRequiredService<FenetrePrincipaleVueModele>();
            MainWindow = fenetre;

            // À partir d'ici, fermer la fenêtre principale ferme le logiciel.
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            fenetre.Show();

            _hote.Services.GetRequiredService<IServiceNavigation>()
                .Naviguer<TableauDeBordVueModele>();
        }
        catch (Exception erreur)
        {
            Log.Fatal(erreur, "Le démarrage de CeramiPro a échoué.");

            MessageBox.Show(
                "CeramiPro n'a pas pu démarrer.\n\n" +
                "Le détail de l'erreur est enregistré dans :\n" +
                Path.Combine(DossierDonnees, "journaux"),
                "CeramiPro", MessageBoxButton.OK, MessageBoxImage.Error);

            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_hote is not null)
        {
            await _hote.StopAsync(TimeSpan.FromSeconds(5));
            _hote.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    /// <summary>
    /// Demande les identifiants avant d'ouvrir l'atelier. Fermer la fenêtre
    /// de connexion ferme le logiciel : rien n'est accessible sans session.
    /// </summary>
    private bool OuvrirSession()
    {
        // La connexion vit dans sa propre portée : son contexte de base de
        // données est refermé une fois les identifiants vérifiés.
        using var portee = _hote!.Services.CreateScope();

        var connexion = new FenetreConnexion
        {
            DataContext = portee.ServiceProvider.GetRequiredService<ConnexionVueModele>()
        };

        var vue = (ConnexionVueModele)connexion.DataContext;
        vue.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ConnexionVueModele.ConnexionReussie) && vue.ConnexionReussie)
            {
                connexion.DialogResult = true;
            }
        };

        if (connexion.ShowDialog() != true)
        {
            return false;
        }

        // Un compte créé par un administrateur reçoit un mot de passe
        // provisoire, communiqué de vive voix : il ne doit pas rester en
        // usage. L'atelier ne s'ouvre qu'une fois qu'il a été remplacé.
        return vue.Profil is not { DoitChangerMotDePasse: true } || ChangerLeMotDePasse();
    }

    /// <summary>Impose le remplacement d'un mot de passe provisoire.</summary>
    private bool ChangerLeMotDePasse()
    {
        using var portee = _hote!.Services.CreateScope();

        var vueModele = portee.ServiceProvider.GetRequiredService<ChangementMotDePasseVueModele>();
        vueModele.Obligatoire = true;

        var fenetre = new FenetreMotDePasse { DataContext = vueModele };

        if (fenetre.ShowDialog() == true)
        {
            return true;
        }

        MessageBox.Show(
            "Le mot de passe provisoire de ce compte doit être remplacé avant\n" +
            "d'ouvrir l'atelier.\n\n" +
            "Relancez CeramiPro pour réessayer.",
            "CeramiPro", MessageBoxButton.OK, MessageBoxImage.Information);

        return false;
    }

    private static IHost ConstruireHote()
    {
        VerifierReglagesLocaux();

        return Host.CreateDefaultBuilder()
        .UseContentRoot(AppContext.BaseDirectory)
        // La vérification à la construction reste active : elle signale un
        // service impossible à fabriquer avant que l'utilisateur ne clique.
        //
        // Le contrôle des portées, lui, est désactivé : quelques fenêtres —
        // la connexion, le changement de mot de passe — sont ouvertes depuis
        // le conteneur racine, chacune dans sa propre portée créée à la main
        // juste avant. Les écrans, eux, passent par le service de navigation,
        // qui leur en donne une.
        .UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = false;
        })
        .ConfigureAppConfiguration((contexte, configuration) => configuration
            .SetBasePath(AppContext.BaseDirectory)
            // Réglages livrés avec le logiciel, sans mot de passe.
            .AddJsonFile(FichierReglages, optional: false)
            // Réglages propres à cet ordinateur : mot de passe de la base,
            // dossier de sauvegarde. Ce fichier n'est jamais versionné et il
            // est toujours lu, quel que soit le mode de lancement. Il n'est
            // ajouté que s'il est exploitable : un fichier vide ou mal écrit
            // doit donner un message clair, pas une erreur technique.
            .AddJsonFile(FichierReglagesLocaux,
                optional: true, reloadOnChange: false)
            // Le même fichier, mais dans le dossier de données de
            // l'utilisateur. C'est celui que l'installateur crée : sous
            // « Program Files », modifier un fichier demande des droits
            // d'administrateur que l'atelier n'a pas toujours.
            .AddJsonFile(Path.Combine(DossierDonnees, FichierReglagesLocaux),
                optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("CERAMIPRO_"))
        .UseSerilog((contexte, journalisation) => journalisation
            .ReadFrom.Configuration(contexte.Configuration)
            .WriteTo.File(
                Path.Combine(DossierDonnees, "journaux", "ceramipro-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                    "{Timestamp:dd/MM/yyyy HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
        .ConfigureServices((contexte, services) =>
        {
            services.AjouterApplication();
            services.AjouterInfrastructure(contexte.Configuration);

            // Ce que seule une application Windows peut fournir : les boîtes
            // de dialogue et les fenêtres du système.
            services.AddSingleton<IServiceLangue, ServiceLangue>();
            services.AddSingleton<IServiceDialogue, ServiceDialogue>();
            services.AddSingleton<IServiceFormulaire, ServiceFormulaire>();
            services.AddSingleton<IServiceFichier, ServiceFichier>();
            services.AddSingleton<IServiceCompte, ServiceCompte>();
            services.AddSingleton<FenetrePrincipale>();

            // Les écrans et les formulaires sont déclarés dans la couche
            // présentation, où les tests vérifient qu'aucun ne manque.
            services.AjouterPresentation();
        })
            .Build();
    }

    /// <summary>
    /// Vérifie que PostgreSQL répond avant d'ouvrir la fenêtre. Un message
    /// clair vaut mieux qu'une application qui s'ouvre puis échoue partout.
    /// </summary>
    private async Task<bool> BaseDeDonneesPreteAsync()
    {
        using var portee = _hote!.Services.CreateScope();
        var contexte = portee.ServiceProvider.GetRequiredService<CeramiProDbContext>();

        try
        {
            await contexte.Database.MigrateAsync();

            var semeur = portee.ServiceProvider.GetRequiredService<CeramiPro.Infrastructure.Data.Seed.DatabaseSeeder>();
            await semeur.ExecuterAsync();

            Log.Information("Base de données « {Base} » prête.", ParametresAtelier.NomBaseDeDonnees);
            return true;
        }
        catch (Exception erreur)
        {
            Log.Error(erreur, "La base de données est injoignable.");

            MessageBox.Show(
                "Impossible de se connecter à la base de données.\n\n" +
                "Vérifiez, dans l'ordre :\n\n" +
                "1. Le service PostgreSQL est démarré.\n" +
                $"2. Le fichier « {FichierReglagesLocaux} » existe et\n" +
                "    contient le bon mot de passe.\n\n" +
                "Il est cherché à ces deux endroits, le second l'emportant :\n\n" +
                Path.Combine(AppContext.BaseDirectory, FichierReglagesLocaux) + "\n" +
                Path.Combine(DossierDonnees, FichierReglagesLocaux),
                "CeramiPro", MessageBoxButton.OK, MessageBoxImage.Warning);

            return false;
        }
    }
}
