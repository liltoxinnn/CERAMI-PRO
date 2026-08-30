using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Application.Services;
using CeramicWorkshop.Domain.Entities.Materials;
using CeramicWorkshop.Domain.Entities.Suppliers;
using CeramicWorkshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.UnitTests.Aides;

/// <summary>
/// Assemble les services du module stock sur une base en mémoire, avec un jeu
/// de données minimal : une catégorie, une unité, un fournisseur et une matière.
/// </summary>
public class AtelierDeTest : IDisposable
{
    public AtelierDeTest()
    {
        UtilisateurCourant = new UtilisateurCourantFactice { UserId = 1, UserName = "admin" };
        Horloge = new HorlogeFactice();
        Audit = new AuditFactice();
        Contexte = ContexteTest.Creer(UtilisateurCourant, Horloge);

        Numerotation = new ReferenceNumberService(Contexte, Horloge);
        Inventaire = new InventaireService(Contexte, UtilisateurCourant, Horloge, Numerotation, Audit);
        Matieres = new MatiereService(Contexte, Inventaire, Numerotation, Audit);
        Fournisseurs = new FournisseurService(Contexte, Numerotation, UtilisateurCourant, Horloge, Audit);
        Achats = new AchatService(Contexte, Inventaire, Numerotation, UtilisateurCourant, Horloge, Audit);

        PreparerReferentiel();
    }

    public ApplicationDbContext Contexte { get; }
    public UtilisateurCourantFactice UtilisateurCourant { get; }
    public HorlogeFactice Horloge { get; }
    public AuditFactice Audit { get; }
    public IReferenceNumberService Numerotation { get; }
    public IInventaireService Inventaire { get; }
    public IMatiereService Matieres { get; }
    public IFournisseurService Fournisseurs { get; }
    public IAchatService Achats { get; }

    public int CategorieId { get; private set; }
    public int UniteKiloId { get; private set; }
    public int FournisseurId { get; private set; }
    public int ModeReglementId { get; private set; }

    private void PreparerReferentiel()
    {
        var categorie = new MaterialCategory { Name = "Argile" };
        var unite = new Unit { Code = "kg", Name = "Kilogramme", Type = Domain.Enums.UnitType.Poids };
        var fournisseur = new Supplier { SupplierNumber = "FRN-2026-0001", Name = "Poterie du Sud" };
        var mode = new Domain.Entities.Payments.PaymentMethod { Code = "especes", Name = "Espèces" };

        Contexte.MaterialCategories.Add(categorie);
        Contexte.Units.Add(unite);
        Contexte.Suppliers.Add(fournisseur);
        Contexte.PaymentMethods.Add(mode);
        Contexte.SaveChanges();

        CategorieId = categorie.Id;
        UniteKiloId = unite.Id;
        FournisseurId = fournisseur.Id;
        ModeReglementId = mode.Id;
    }

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
