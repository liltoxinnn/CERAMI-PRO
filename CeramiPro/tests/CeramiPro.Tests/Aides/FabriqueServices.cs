using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Services;
using CeramiPro.Domain.Entities.Catalog;
using CeramiPro.Domain.Entities.Expenses;
using CeramiPro.Domain.Entities.Payments;
using CeramiPro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Tests.Aides;

/// <summary>
/// Les services de l'atelier assemblés sur une base en mémoire, avec le
/// minimum pour travailler : une catégorie de produits, une catégorie de
/// dépenses et un mode de règlement.
/// </summary>
public class AtelierDeTest : IDisposable
{
    public AtelierDeTest()
    {
        UtilisateurCourant = new UtilisateurCourantFactice();
        Horloge = new HorlogeFactice();
        Audit = new AuditFactice();
        Contexte = ContexteTest.Creer(UtilisateurCourant, Horloge);

        Numerotation = new ReferenceNumberService(Contexte, Horloge);
        Inventaire = new InventaireService(Contexte, UtilisateurCourant, Horloge, Audit);
        Produits = new ProduitService(Contexte, Inventaire, Numerotation, Audit);
        Clients = new ClientService(Contexte, Numerotation, UtilisateurCourant, Audit, Horloge);
        Commandes = new CommandeService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        Paiements = new PaiementService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        Ventes = new VenteService(Contexte, Inventaire, Paiements, Numerotation, UtilisateurCourant, Horloge, Audit);
        Depenses = new DepenseService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        Parametres = new ParametresService(Contexte, Audit);
        Referentiels = new ReferentielService(Contexte, Audit);
        TableauDeBord = new TableauDeBordService(Contexte, Horloge);
        Rapports = new RapportService(Contexte, Horloge);

        PreparerReferentiel();
    }

    public CeramiProDbContext Contexte { get; }
    public UtilisateurCourantFactice UtilisateurCourant { get; }
    public HorlogeFactice Horloge { get; }
    public AuditFactice Audit { get; }
    public IReferenceNumberService Numerotation { get; }
    public IInventaireService Inventaire { get; }
    public IProduitService Produits { get; }
    public IClientService Clients { get; }
    public ICommandeService Commandes { get; }
    public IPaiementService Paiements { get; }
    public IVenteService Ventes { get; }
    public IDepenseService Depenses { get; }
    public IParametresService Parametres { get; }
    public IReferentielService Referentiels { get; }
    public ITableauDeBordService TableauDeBord { get; }
    public IRapportService Rapports { get; }

    public int ModeReglementId { get; private set; }
    public int CategorieProduitId { get; private set; }
    public int CategorieDepenseId { get; private set; }

    private void PreparerReferentiel()
    {
        var mode = new PaymentMethod { Code = "especes", Name = "Espèces" };
        var categorieProduit = new ProductCategory { Name = "Vases décoratifs" };
        var categorieDepense = new ExpenseCategory { Name = "Électricité" };

        Contexte.PaymentMethods.Add(mode);
        Contexte.ProductCategories.Add(categorieProduit);
        Contexte.ExpenseCategories.Add(categorieDepense);
        Contexte.SaveChanges();

        ModeReglementId = mode.Id;
        CategorieProduitId = categorieProduit.Id;
        CategorieDepenseId = categorieDepense.Id;
    }

    /// <summary>Crée un produit avec son stock de départ.</summary>
    public async Task<int> CreerProduitAsync(string nom, decimal prixVente = 3500m,
        decimal coutProduction = 1850m, decimal stockInitial = 0, decimal stockMinimum = 2m,
        bool pieceUnique = false)
        => (await Produits.CreerAsync(new ProduitRequete
        {
            Nom = nom,
            CategorieId = CategorieProduitId,
            PrixVente = prixVente,
            CoutProduction = coutProduction,
            StockMinimum = stockMinimum,
            StockInitial = stockInitial,
            PieceUnique = pieceUnique
        })).Id;

    public async Task<decimal> StockProduitAsync(int produitId)
        => (await Contexte.Products.AsNoTracking().FirstAsync(p => p.Id == produitId)).CurrentStock;

    /// <summary>Crée un client de test.</summary>
    public async Task<int> CreerClientAsync(string nom = "Mohamed Benali")
        => (await Clients.CreerAsync(new ClientRequete { Nom = nom, Telephone = "0550 11 22 33" })).Id;

    /// <summary>Enregistre une vente simple, payée ou non.</summary>
    public Task<VenteDto> VendreAsync(int produitId, decimal quantite = 1m, decimal prix = 3500m,
        decimal paye = 0m, int? clientId = null)
        => Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            MontantPaye = paye,
            ModeReglementId = paye > 0 ? ModeReglementId : null,
            Lignes = new List<LigneVenteRequete>
            {
                new() { ProduitId = produitId, Quantite = quantite, PrixUnitaire = prix }
            }
        });

    /// <summary>Crée une commande personnalisée à livrer dans dix jours.</summary>
    public Task<CommandeDto> CommanderAsync(int clientId, decimal prixUnitaire = 12000m,
        Domain.Enums.CanalCommande canal = Domain.Enums.CanalCommande.Instagram)
        => Commandes.CreerAsync(new CommandeRequete
        {
            ClientId = clientId,
            Titre = "Service à thé bleu",
            Canal = canal,
            Quantite = 1m,
            PrixUnitaire = prixUnitaire,
            DateLimite = Horloge.Aujourdhui.AddDays(10)
        });

    /// <summary>Enregistre un paiement sur une commande.</summary>
    public Task<PaiementDto> EncaisserCommandeAsync(int commandeId, decimal montant, bool acompte = true)
        => Paiements.EnregistrerAsync(new PaiementRequete
        {
            CommandeId = commandeId,
            Montant = montant,
            ModeReglementId = ModeReglementId,
            Acompte = acompte
        });

    /// <summary>Enregistre une dépense de test.</summary>
    public async Task<int> CreerDepenseAsync(
        decimal montant, string description = "Facture d'électricité", DateTime? date = null)
        => (await Depenses.CreerAsync(new DepenseRequete
        {
            CategorieId = CategorieDepenseId,
            Montant = montant,
            Description = description,
            Date = date
        })).Id;

    public void Dispose() => Contexte.Dispose();
}
