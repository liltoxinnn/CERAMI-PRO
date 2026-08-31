using CeramiPro.Domain.Common;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Services;
using CeramiPro.Domain.Entities.Materials;
using CeramiPro.Domain.Entities.Suppliers;
using CeramiPro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Tests.Aides;

/// <summary>
/// Assemble les services du module stock sur une base en mémoire, avec un jeu
/// de données minimal : une catégorie, une unité, un fournisseur et une matière.
/// </summary>
public class AtelierDeTest : IDisposable
{
    public AtelierDeTest()
    {
        UtilisateurCourant = new UtilisateurCourantFactice
        {
            UtilisateurId = 1,
            NomUtilisateur = "admin",
            CodeRole = RoleCodes.Administrateur
        };

        Horloge = new HorlogeFactice();
        Audit = new AuditFactice();
        Contexte = ContexteTest.Creer(UtilisateurCourant, Horloge);

        Numerotation = new ReferenceNumberService(Contexte, Horloge);
        Inventaire = new InventaireService(Contexte, UtilisateurCourant, Horloge, Numerotation, Audit);
        Matieres = new MatiereService(Contexte, Inventaire, Numerotation, Audit);
        Fournisseurs = new FournisseurService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        Achats = new AchatService(Contexte, Inventaire, Numerotation, UtilisateurCourant, Horloge, Audit);
        Produits = new ProduitService(Contexte, Inventaire, Numerotation, Audit);
        Recettes = new RecetteService(Contexte, Audit);
        Production = new ProductionService(
            Contexte, Inventaire, Recettes, Numerotation, UtilisateurCourant, Horloge, Audit);
        Fours = new FourService(Contexte, Audit);
        Cuissons = new CuissonService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        Decorations = new DecorationService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        Qualite = new QualiteService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        Clients = new ClientService(Contexte, Numerotation, UtilisateurCourant, Audit);
        Commandes = new CommandeService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        Paiements = new PaiementService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        Factures = new FactureService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        Ventes = new VenteService(
            Contexte, Inventaire, Paiements, Numerotation, UtilisateurCourant, Horloge, Audit);
        Depenses = new DepenseService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        TableauDeBord = new TableauDeBordService(Contexte, Horloge);
        Rapports = new RapportService(Contexte, Horloge);
        Calculateurs = new CalculateurService();
        Images = new CeramiPro.Infrastructure.Services.CodeGraphiqueService();
        Codes = new CodeService(Contexte, Images, UtilisateurCourant);
        Recherche = new RechercheService(Contexte, UtilisateurCourant);
        Alertes = new AlerteService(Contexte, Horloge);

        PreparerReferentiel();
    }

    public CeramiProDbContext Contexte { get; }
    public UtilisateurCourantFactice UtilisateurCourant { get; }
    public HorlogeFactice Horloge { get; }
    public AuditFactice Audit { get; }
    public IReferenceNumberService Numerotation { get; }
    public IInventaireService Inventaire { get; }
    public IMatiereService Matieres { get; }
    public IFournisseurService Fournisseurs { get; }
    public IAchatService Achats { get; }
    public IProduitService Produits { get; }
    public IRecetteService Recettes { get; }
    public IProductionService Production { get; }
    public IFourService Fours { get; }
    public ICuissonService Cuissons { get; }
    public IDecorationService Decorations { get; }
    public IQualiteService Qualite { get; }
    public IClientService Clients { get; }
    public ICommandeService Commandes { get; }
    public IPaiementService Paiements { get; }
    public IFactureService Factures { get; }
    public IVenteService Ventes { get; }
    public IDepenseService Depenses { get; }
    public ITableauDeBordService TableauDeBord { get; }
    public IRapportService Rapports { get; }
    public ICalculateurService Calculateurs { get; }
    public ICodeGraphiqueService Images { get; }
    public ICodeService Codes { get; }
    public IRechercheService Recherche { get; }
    public IAlerteService Alertes { get; }

    public int CategorieId { get; private set; }
    public int UniteKiloId { get; private set; }
    public int FournisseurId { get; private set; }
    public int ModeReglementId { get; private set; }
    public int CategorieProduitId { get; private set; }
    public int CategorieDepenseId { get; private set; }

    private void PreparerReferentiel()
    {
        var categorie = new MaterialCategory { Name = "Argile" };
        var unite = new Unit { Code = "kg", Name = "Kilogramme", Type = Domain.Enums.UnitType.Poids };
        var fournisseur = new Supplier { SupplierNumber = "FRN-2026-0001", Name = "Poterie du Sud" };
        var mode = new Domain.Entities.Payments.PaymentMethod { Code = "especes", Name = "Espèces" };
        var categorieProduit = new Domain.Entities.Catalog.ProductCategory { Name = "Vases décoratifs" };
        var categorieDepense = new Domain.Entities.Expenses.ExpenseCategory { Name = "Électricité" };

        Contexte.MaterialCategories.Add(categorie);
        Contexte.Units.Add(unite);
        Contexte.Suppliers.Add(fournisseur);
        Contexte.PaymentMethods.Add(mode);
        Contexte.ProductCategories.Add(categorieProduit);
        Contexte.ExpenseCategories.Add(categorieDepense);
        Contexte.SaveChanges();

        CategorieId = categorie.Id;
        UniteKiloId = unite.Id;
        FournisseurId = fournisseur.Id;
        ModeReglementId = mode.Id;
        CategorieProduitId = categorieProduit.Id;
        CategorieDepenseId = categorieDepense.Id;
    }

    /// <summary>Enregistre une dépense de test.</summary>
    public async Task<int> CreerDepenseAsync(
        decimal montant, string description = "Facture d'électricité", DateTime? date = null)
        => (await Depenses.CreerAsync(new Application.DTOs.Finances.DepenseRequete
        {
            CategorieId = CategorieDepenseId,
            Montant = montant,
            Description = description,
            Date = date ?? Horloge.MaintenantUtc
        })).Id;

    /// <summary>Crée les réglages d'alertes, comme le fait l'amorçage de la base.</summary>
    public void PreparerAlertes()
    {
        var types = new[]
        {
            Domain.Enums.NotificationType.StockFaible,
            Domain.Enums.NotificationType.MatiereInsuffisante,
            Domain.Enums.NotificationType.CommandeEcheance,
            Domain.Enums.NotificationType.CommandeRetard,
            Domain.Enums.NotificationType.PaiementEnAttente,
            Domain.Enums.NotificationType.DetteClient,
            Domain.Enums.NotificationType.DetteFournisseur,
            Domain.Enums.NotificationType.ProductionBloquee,
            Domain.Enums.NotificationType.ProductionRetard,
            Domain.Enums.NotificationType.AttenteProlongee
        };

        foreach (var type in types)
        {
            Contexte.NotificationSettings.Add(new Domain.Entities.Notifications.NotificationSetting
            {
                Type = type,
                IsEnabled = true,
                ThresholdDays = type is Domain.Enums.NotificationType.CommandeEcheance ? 3 : 7
            });
        }

        Contexte.SaveChanges();
    }

    /// <summary>
    /// Donne tous les droits à l'utilisateur de test. Par défaut il n'en a
    /// aucun, ce qui permet de vérifier les refus ; les tests qui ont besoin
    /// d'un administrateur complet appellent cette méthode.
    /// </summary>
    public void AccorderTousLesDroits()
    {
        foreach (var droit in PermissionCodes.Catalogue)
        {
            UtilisateurCourant.Droits.Add(droit.Code);
        }
    }

    /// <summary>Crée un produit fini avec son stock de départ.</summary>
    public async Task<int> CreerProduitAsync(string nom, decimal prixVente = 3500m,
        decimal coutProduction = 1850m, decimal stockInitial = 0, decimal stockMinimum = 2m)
    {
        var produit = await Produits.CreerAsync(new Application.DTOs.Catalogue.ProduitRequete
        {
            Nom = nom,
            CategorieId = CategorieProduitId,
            PrixVente = prixVente,
            CoutProduction = coutProduction,
            StockMinimum = stockMinimum,
            StockInitial = stockInitial
        });

        return produit.Id;
    }

    public async Task<decimal> StockProduitAsync(int produitId)
        => (await Contexte.Products.AsNoTracking().FirstAsync(p => p.Id == produitId)).CurrentStock;

    /// <summary>
    /// Prépare un produit, ses matières et une recette : 1,5 kg d'argile et
    /// 0,1 kg d'émail par pièce, plus les frais de fabrication.
    /// </summary>
    public async Task<(int ProduitId, int RecetteId, int ArgileId)> PreparerVaseAsync(
        decimal stockArgile = 100m, decimal stockEmail = 10m)
    {
        var produitId = await CreerProduitAsync("Vase décoratif A", prixVente: 3500m, coutProduction: 0m);
        var argileId = await CreerMatiereAsync("Argile", stockInitial: stockArgile, prix: 200m);
        var emailId = await CreerMatiereAsync("Émail", stockInitial: stockEmail, prix: 2500m);

        var recette = await Recettes.CreerAsync(new Application.DTOs.Catalogue.RecetteRequete
        {
            ProduitId = produitId,
            Nom = "Vase décoratif A",
            Rendement = 1m,
            CoutMainOeuvre = 600m,
            CoutCuisson = 300m,
            CoutEmballage = 50m,
            Lignes = new List<Application.DTOs.Catalogue.LigneRecetteRequete>
            {
                new() { MatiereId = argileId, UniteId = UniteKiloId, Quantite = 1.5m },
                new() { MatiereId = emailId, UniteId = UniteKiloId, Quantite = 0.1m }
            }
        });

        return (produitId, recette.Id, argileId);
    }

    /// <summary>Crée un client de test.</summary>
    public async Task<int> CreerClientAsync(string nom = "Mohamed Benali")
        => (await Clients.CreerAsync(new Application.DTOs.Commercial.ClientRequete
        {
            Nom = nom, Telephone = "0550 11 22 33"
        })).Id;

    /// <summary>Enregistre un contrôle qualité conforme sur une production.</summary>
    public Task ControlerAsync(int productionId, decimal quantite)
        => Qualite.EnregistrerAsync(new Application.DTOs.Production.ControleQualiteRequete
        {
            ProductionId = productionId,
            QuantiteControlee = quantite,
            QuantiteAcceptee = quantite
        });

    /// <summary>Crée une matière avec un stock de départ.</summary>
    public async Task<int> CreerMatiereAsync(string nom, decimal stockInitial = 0, decimal prix = 100m,
        decimal stockMinimum = 5m)
    {
        var matiere = await Matieres.CreerAsync(new Application.DTOs.Stock.MatiereRequete
        {
            Nom = nom,
            CategorieId = CategorieId,
            UniteId = UniteKiloId,
            StockMinimum = stockMinimum,
            PrixAchat = prix,
            StockInitial = stockInitial
        });

        return matiere.Id;
    }

    public async Task<decimal> StockMatiereAsync(int matiereId)
        => (await Contexte.Materials.AsNoTracking().FirstAsync(m => m.Id == matiereId)).CurrentQuantity;

    public void Dispose() => Contexte.Dispose();
}
