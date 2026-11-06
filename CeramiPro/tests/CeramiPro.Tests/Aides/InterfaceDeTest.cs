using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Services;
using CeramiPro.Infrastructure.Authentication;
using CeramiPro.Infrastructure.Services;
using CeramiPro.Presentation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Tests.Aides;

/// <summary>
/// Les écrans de l'application, construits comme dans le logiciel — par
/// l'injection de dépendances — au-dessus des vrais services métier d'un
/// <see cref="AtelierDeTest"/>. Seul ce qui touche à Windows (boîtes de
/// dialogue, fenêtres, fichiers) est remplacé par des doubles.
///
/// Un bouton testé ici fait donc exactement ce qu'il ferait au clic : il
/// ouvre le bon formulaire, appelle le bon service, et le stock bouge.
/// </summary>
public sealed class InterfaceDeTest : IDisposable
{
    private readonly ServiceProvider _fournisseur;

    public InterfaceDeTest()
    {
        DossierPhotos = Path.Combine(Path.GetTempPath(), $"ceramipro-photos-{Guid.NewGuid():N}");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Photos:Dossier"] = DossierPhotos })
            .Build();

        var services = new ServiceCollection();
        var a = Atelier;

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IApplicationDbContext>(a.Contexte);
        services.AddSingleton<IUtilisateurCourant>(a.UtilisateurCourant);
        services.AddSingleton<ISessionAtelier>(a.UtilisateurCourant);
        services.AddSingleton<IServiceDateHeure>(a.Horloge);
        services.AddSingleton<IAuditService>(a.Audit);
        services.AddSingleton(a.Numerotation);
        services.AddSingleton(a.Inventaire);
        services.AddSingleton(a.Produits);
        services.AddSingleton(a.Clients);
        services.AddSingleton(a.Commandes);
        services.AddSingleton(a.Paiements);
        services.AddSingleton(a.Ventes);
        services.AddSingleton(a.Depenses);
        services.AddSingleton(a.Parametres);
        services.AddSingleton(a.Referentiels);
        services.AddSingleton(a.TableauDeBord);
        services.AddSingleton(a.Rapports);
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<IUtilisateurService, UtilisateurService>();
        services.AddSingleton<IDocumentService, DocumentService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IStockagePhotos, StockagePhotos>();
        services.AddSingleton<IServiceEtatBaseDeDonnees>(BaseDeDonnees);

        services.AddSingleton<IServiceDialogue>(Dialogue);
        services.AddSingleton<IServiceFormulaire>(Formulaires);
        services.AddSingleton<IServiceFichier>(Fichiers);
        services.AddSingleton<IServiceFenetres>(Fenetres);
        services.AddSingleton<IServiceCompte, CompteFactice>();

        services.AjouterPresentation();

        _fournisseur = services.BuildServiceProvider();
    }

    public AtelierDeTest Atelier { get; } = new();

    public DialogueFactice Dialogue { get; } = new();

    public FormulaireFactice Formulaires { get; } = new();

    public FichierFactice Fichiers { get; } = new();

    public FenetresFactice Fenetres { get; } = new();

    public EtatBaseFactice BaseDeDonnees { get; } = new();

    public string DossierPhotos { get; }

    /// <summary>Construit un écran ou un formulaire comme le ferait le menu.</summary>
    public T Obtenir<T>() where T : notnull => _fournisseur.GetRequiredService<T>();

    /// <summary>Construit l'écran et charge ses données, comme à l'ouverture.</summary>
    public async Task<T> OuvrirAsync<T>() where T : VueModeleBase
    {
        var ecran = Obtenir<T>();
        await ecran.ChargerAsync();
        return ecran;
    }

    public void Dispose()
    {
        _fournisseur.Dispose();
        Atelier.Dispose();

        if (Directory.Exists(DossierPhotos))
        {
            Directory.Delete(DossierPhotos, recursive: true);
        }
    }
}

/// <summary>Compte simulé : on retient les demandes de l'utilisateur.</summary>
public class CompteFactice : IServiceCompte
{
    public int ChangementsDemandes { get; private set; }

    public int Deconnexions { get; private set; }

    public void ChangerMotDePasse() => ChangementsDemandes++;

    public void Deconnecter() => Deconnexions++;
}
