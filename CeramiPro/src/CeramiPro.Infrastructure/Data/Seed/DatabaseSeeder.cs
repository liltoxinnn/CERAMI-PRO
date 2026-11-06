using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Entities.Catalog;
using CeramiPro.Domain.Entities.Expenses;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Payments;
using CeramiPro.Domain.Entities.Settings;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Infrastructure.Data.Seed;

/// <summary>
/// Prépare la base à la première utilisation : compte administrateur, modes
/// de règlement, catégories de départ, identité de l'atelier et réglages.
/// L'opération est réexécutable sans risque : rien n'est dupliqué ni écrasé,
/// et aucune donnée saisie par l'atelier n'est jamais modifiée.
/// </summary>
public class DatabaseSeeder
{
    /// <summary>Identifiant du compte administrateur créé à l'installation.</summary>
    public const string NomUtilisateurAdministrateur = "admin";

    /// <summary>Mot de passe utilisé si aucun n'est fourni dans la configuration.</summary>
    public const string MotDePasseAdministrateurParDefaut = "CeramiPro@2026";

    /// <summary>Clé de configuration permettant d'imposer le mot de passe initial.</summary>
    public const string CleMotDePasseInitial = "Administrateur:MotDePasseInitial";

    /// <summary>
    /// Clé de dépannage : mise à « true », elle redonne au compte
    /// administrateur le mot de passe initial au prochain démarrage.
    ///
    /// Un mot de passe haché ne se retrouve pas ; sans cette porte de secours,
    /// un oubli rendrait le logiciel définitivement inutilisable. Elle exige
    /// d'écrire dans un fichier de l'ordinateur de l'atelier — quiconque le
    /// peut a déjà la main sur la machine et sur la base de données.
    /// </summary>
    public const string CleReinitialisation = "Administrateur:ReinitialiserMotDePasse";

    private readonly CeramiProDbContext _context;
    private readonly IPasswordHasherService _hachage;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _journal;

    public DatabaseSeeder(
        CeramiProDbContext context,
        IPasswordHasherService hachage,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> journal)
    {
        _context = context;
        _hachage = hachage;
        _configuration = configuration;
        _journal = journal;
    }

    public async Task ExecuterAsync(CancellationToken cancellationToken = default)
    {
        await SemerAdministrateurAsync(cancellationToken);
        await SemerModesDeReglementAsync(cancellationToken);
        await SemerCategoriesProduitsAsync(cancellationToken);
        await SemerCategoriesDepensesAsync(cancellationToken);
        await SemerParametresAtelierAsync(cancellationToken);
        await SemerReglagesSystemeAsync(cancellationToken);
    }

    private async Task SemerAdministrateurAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
        {
            await ReinitialiserAdministrateurSiDemandeAsync(cancellationToken);
            return;
        }

        var motDePasseConfigure = _configuration[CleMotDePasseInitial];
        var motDePasse = string.IsNullOrWhiteSpace(motDePasseConfigure)
            ? MotDePasseAdministrateurParDefaut
            : motDePasseConfigure;

        _context.Users.Add(new User
        {
            UserName = NomUtilisateurAdministrateur,
            FullName = "Administrateur de l'atelier",
            PasswordHash = _hachage.Hacher(motDePasse),
            Role = RoleUtilisateur.Administrateur,
            IsActive = true,
            MustChangePassword = true
        });

        await _context.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(motDePasseConfigure))
        {
            _journal.LogWarning(
                "Compte administrateur « {Utilisateur} » créé avec le mot de passe par défaut. " +
                "Changez-le dès la première connexion.", NomUtilisateurAdministrateur);
        }
        else
        {
            _journal.LogInformation("Compte administrateur « {Utilisateur} » créé.", NomUtilisateurAdministrateur);
        }
    }

    /// <summary>
    /// Redonne au compte administrateur le mot de passe initial lorsque la
    /// configuration le demande, puis exige qu'il soit changé à la connexion.
    /// </summary>
    private async Task ReinitialiserAdministrateurSiDemandeAsync(CancellationToken cancellationToken)
    {
        if (!bool.TryParse(_configuration[CleReinitialisation], out var demande) || !demande)
        {
            return;
        }

        var administrateur = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == NomUtilisateurAdministrateur, cancellationToken);

        if (administrateur is null)
        {
            return;
        }

        var motDePasse = _configuration[CleMotDePasseInitial];
        motDePasse = string.IsNullOrWhiteSpace(motDePasse)
            ? MotDePasseAdministrateurParDefaut
            : motDePasse;

        administrateur.PasswordHash = _hachage.Hacher(motDePasse);
        administrateur.MustChangePassword = true;
        administrateur.IsActive = true;

        // Un compte bloqué par des essais répétés doit redevenir utilisable.
        administrateur.FailedLoginAttempts = 0;
        administrateur.LockedUntil = null;

        await _context.SaveChangesAsync(cancellationToken);

        _journal.LogWarning(
            "Le mot de passe du compte « {Utilisateur} » a été réinitialisé à la demande. " +
            "Retirez « {Cle} » de la configuration, puis changez ce mot de passe.",
            NomUtilisateurAdministrateur, CleReinitialisation);
    }

    private async Task SemerModesDeReglementAsync(CancellationToken cancellationToken)
    {
        var modes = new (string Code, string Nom, bool Reference)[]
        {
            ("especes", "Espèces", false),
            ("virement", "Virement bancaire", true),
            ("carte", "Carte bancaire", true),
            ("cheque", "Chèque", true),
            ("autre", "Autre", false)
        };

        var existants = await _context.PaymentMethods.Select(m => m.Code).ToListAsync(cancellationToken);

        foreach (var (code, nom, reference) in modes.Where(m => !existants.Contains(m.Code)))
        {
            _context.PaymentMethods.Add(new PaymentMethod
            {
                Code = code,
                Name = nom,
                RequiresReference = reference,
                IsSystem = true
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SemerCategoriesProduitsAsync(CancellationToken cancellationToken)
    {
        var categories = new[]
        {
            "Vases décoratifs", "Statues", "Assiettes décoratives", "Décorations murales",
            "Pots", "Sculptures", "Objets décoratifs", "Pièces artisanales", "Produits personnalisables"
        };

        var existantes = await _context.ProductCategories.Select(c => c.Name).ToListAsync(cancellationToken);

        foreach (var nom in categories.Where(c => !existantes.Contains(c)))
        {
            _context.ProductCategories.Add(new ProductCategory { Name = nom, IsSystem = true });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SemerCategoriesDepensesAsync(CancellationToken cancellationToken)
    {
        var categories = new[]
        {
            "Électricité", "Gaz", "Transport", "Emballage", "Maintenance",
            "Salaires", "Équipement", "Matières", "Autres"
        };

        var existantes = await _context.ExpenseCategories.Select(c => c.Name).ToListAsync(cancellationToken);

        foreach (var nom in categories.Where(c => !existantes.Contains(c)))
        {
            _context.ExpenseCategories.Add(new ExpenseCategory { Name = nom, IsSystem = true });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SemerParametresAtelierAsync(CancellationToken cancellationToken)
    {
        if (await _context.BusinessSettings.AnyAsync(cancellationToken))
        {
            return;
        }

        _context.BusinessSettings.Add(new BusinessSettings
        {
            WorkshopName = "CERAMIPRO",
            ReceiptFooter = "Merci de votre confiance."
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SemerReglagesSystemeAsync(CancellationToken cancellationToken)
    {
        var reglages = new (string Cle, string Valeur, string Categorie, string TypeValeur, string Description, bool AdminSeul)[]
        {
            ("sauvegarde.automatique", "false", "Sauvegarde", "booleen",
                "Activer la sauvegarde automatique de la base de données.", true),
            ("sauvegarde.heure", "22:00", "Sauvegarde", "heure",
                "Heure de déclenchement de la sauvegarde automatique.", true),
            ("sauvegarde.conservation.jours", "30", "Sauvegarde", "entier",
                "Nombre de jours de conservation des sauvegardes.", true),
            ("stock.autoriser.negatif", "false", "Stock", "booleen",
                "Autoriser un stock négatif (déconseillé).", true),
            ("affichage.lignes.par.page", "25", "Affichage", "entier",
                "Nombre de lignes affichées par défaut dans les tableaux.", false)
        };

        var existants = await _context.SystemSettings.Select(s => s.Key).ToListAsync(cancellationToken);

        foreach (var reglage in reglages.Where(r => !existants.Contains(r.Cle)))
        {
            _context.SystemSettings.Add(new SystemSetting
            {
                Key = reglage.Cle,
                Value = reglage.Valeur,
                Category = reglage.Categorie,
                ValueType = reglage.TypeValeur,
                Description = reglage.Description,
                IsAdminOnly = reglage.AdminSeul
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
