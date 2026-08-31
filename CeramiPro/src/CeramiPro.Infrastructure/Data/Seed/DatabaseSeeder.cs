using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Catalog;
using CeramiPro.Domain.Entities.Decoration;
using CeramiPro.Domain.Entities.Expenses;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Materials;
using CeramiPro.Domain.Entities.Notifications;
using CeramiPro.Domain.Entities.Payments;
using CeramiPro.Domain.Entities.Settings;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Infrastructure.Data.Seed;

/// <summary>
/// Prépare la base à la première utilisation : rôles, droits, compte administrateur,
/// unités de mesure, modes de règlement et catégories de départ.
/// L'opération est réexécutable sans risque : rien n'est dupliqué ni écrasé.
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
        await SemerDroitsAsync(cancellationToken);
        await SemerRolesAsync(cancellationToken);
        await SemerDroitsDesRolesAsync(cancellationToken);
        await SemerAdministrateurAsync(cancellationToken);
        await SemerUnitesAsync(cancellationToken);
        await SemerModesDeReglementAsync(cancellationToken);
        await SemerCategoriesMatieresAsync(cancellationToken);
        await SemerCategoriesProduitsAsync(cancellationToken);
        await SemerCategoriesDepensesAsync(cancellationToken);
        await SemerTypesDecorationAsync(cancellationToken);
        await SemerParametresAtelierAsync(cancellationToken);
        await SemerReglagesAlertesAsync(cancellationToken);
        await SemerReglagesSystemeAsync(cancellationToken);
    }

    private async Task SemerDroitsAsync(CancellationToken cancellationToken)
    {
        var existants = await _context.Permissions.ToDictionaryAsync(p => p.Code, cancellationToken);

        foreach (var definition in PermissionCodes.Catalogue)
        {
            if (existants.TryGetValue(definition.Code, out var droit))
            {
                // Les libellés peuvent évoluer d'une version à l'autre.
                droit.Name = definition.Nom;
                droit.Module = definition.Module;
                continue;
            }

            _context.Permissions.Add(new Permission
            {
                Code = definition.Code,
                Name = definition.Nom,
                Module = definition.Module
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SemerRolesAsync(CancellationToken cancellationToken)
    {
        var existants = await _context.Roles.Select(r => r.Code).ToListAsync(cancellationToken);

        foreach (var (code, nom, description) in RoleCodes.Catalogue.Where(r => !existants.Contains(r.Code)))
        {
            _context.Roles.Add(new Role
            {
                Code = code,
                Name = nom,
                Description = description,
                IsSystem = true
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SemerDroitsDesRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles.Include(r => r.RolePermissions).ToListAsync(cancellationToken);
        var droits = await _context.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken);

        foreach (var role in roles)
        {
            if (!PermissionCodes.DroitsParDefaut.TryGetValue(role.Code, out var codesAttendus))
            {
                continue;
            }

            // L'administrateur reçoit systématiquement tous les droits, y compris ceux
            // ajoutés par une nouvelle version. Les autres rôles ne sont initialisés
            // qu'une seule fois, pour ne pas écraser les réglages de l'atelier.
            if (role.Code != RoleCodes.Administrateur && role.RolePermissions.Count > 0)
            {
                continue;
            }

            var dejaAttribues = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

            foreach (var code in codesAttendus)
            {
                if (droits.TryGetValue(code, out var idDroit) && !dejaAttribues.Contains(idDroit))
                {
                    _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = idDroit });
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SemerAdministrateurAsync(CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
        {
            await ReinitialiserAdministrateurSiDemandeAsync(cancellationToken);
            return;
        }

        var role = await _context.Roles.FirstAsync(r => r.Code == RoleCodes.Administrateur, cancellationToken);

        var motDePasseConfigure = _configuration[CleMotDePasseInitial];
        var motDePasse = string.IsNullOrWhiteSpace(motDePasseConfigure)
            ? MotDePasseAdministrateurParDefaut
            : motDePasseConfigure;

        _context.Users.Add(new User
        {
            UserName = NomUtilisateurAdministrateur,
            FullName = "Administrateur de l'atelier",
            PasswordHash = _hachage.Hacher(motDePasse),
            RoleId = role.Id,
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

    private async Task SemerUnitesAsync(CancellationToken cancellationToken)
    {
        var unites = new (string Code, string Nom, UnitType Type, decimal Facteur)[]
        {
            ("kg", "Kilogramme", UnitType.Poids, 1m),
            ("g", "Gramme", UnitType.Poids, 0.001m),
            ("L", "Litre", UnitType.Volume, 1m),
            ("ml", "Millilitre", UnitType.Volume, 0.001m),
            ("piece", "Pièce", UnitType.Quantite, 1m),
            ("m", "Mètre", UnitType.Longueur, 1m),
            ("m2", "Mètre carré", UnitType.Surface, 1m),
            ("boite", "Boîte", UnitType.Quantite, 1m),
            ("unite", "Unité", UnitType.Quantite, 1m)
        };

        var existantes = await _context.Units.Select(u => u.Code).ToListAsync(cancellationToken);

        foreach (var (code, nom, type, facteur) in unites.Where(u => !existantes.Contains(u.Code)))
        {
            _context.Units.Add(new Unit
            {
                Code = code,
                Name = nom,
                Type = type,
                ConversionFactor = facteur,
                IsSystem = true
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
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

    private async Task SemerCategoriesMatieresAsync(CancellationToken cancellationToken)
    {
        var categories = new[]
        {
            "Argile", "Pâte céramique", "Plâtre", "Émaux", "Pigments", "Peinture",
            "Métaux décoratifs", "Colles", "Emballage", "Autres consommables"
        };

        var existantes = await _context.MaterialCategories.Select(c => c.Name).ToListAsync(cancellationToken);

        foreach (var nom in categories.Where(c => !existantes.Contains(c)))
        {
            _context.MaterialCategories.Add(new MaterialCategory { Name = nom });
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
            _context.ProductCategories.Add(new ProductCategory { Name = nom });
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

    private async Task SemerTypesDecorationAsync(CancellationToken cancellationToken)
    {
        var types = new (string Nom, string Description)[]
        {
            ("Émaillage", "Application d'émail avant la cuisson finale."),
            ("Peinture à la main", "Décor peint pièce par pièce."),
            ("Dorure", "Décor à l'or décoratif."),
            ("Argenture", "Décor à l'argent décoratif."),
            ("Gravure", "Décor gravé dans la matière."),
            ("Décor imprimé", "Application d'un décor par transfert.")
        };

        var existants = await _context.DecorationTypes.Select(t => t.Name).ToListAsync(cancellationToken);

        foreach (var (nom, description) in types.Where(t => !existants.Contains(t.Nom)))
        {
            _context.DecorationTypes.Add(new DecorationType { Name = nom, Description = description });
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
            InvoiceFooter = "Merci de votre confiance."
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SemerReglagesAlertesAsync(CancellationToken cancellationToken)
    {
        var reglages = new (NotificationType Type, int? Jours, decimal? Seuil)[]
        {
            (NotificationType.StockFaible, null, null),
            (NotificationType.MatiereInsuffisante, null, null),
            (NotificationType.CommandeEcheance, 3, null),
            (NotificationType.CommandeRetard, 0, null),
            (NotificationType.PaiementEnAttente, 7, null),
            (NotificationType.DetteClient, 30, null),
            (NotificationType.DetteFournisseur, 30, null),
            (NotificationType.ProductionBloquee, 3, null),
            (NotificationType.ProductionRetard, 0, null),
            (NotificationType.AttenteProlongee, 7, null)
        };

        var existants = await _context.NotificationSettings.Select(s => s.Type).ToListAsync(cancellationToken);

        foreach (var (type, jours, seuil) in reglages.Where(r => !existants.Contains(r.Type)))
        {
            _context.NotificationSettings.Add(new NotificationSetting
            {
                Type = type,
                IsEnabled = true,
                ThresholdDays = jours,
                ThresholdValue = seuil
            });
        }

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
