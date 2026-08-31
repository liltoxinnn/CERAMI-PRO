using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Services;
using CeramiPro.Domain.Entities.Settings;
using CeramiPro.Domain.Enums;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Tests.Stock;

public class InventaireServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    [Fact]
    public async Task Un_mouvement_conserve_le_stock_avant_et_apres()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile rouge", stockInitial: 30m);

        await _atelier.Inventaire.EnregistrerAsync(new MouvementStockRequete
        {
            TypeArticle = InventoryItemType.MatierePremiere,
            TypeMouvement = InventoryTransactionType.ConsommationProduction,
            MatiereId = matiereId,
            Quantite = -12m,
            CoutUnitaire = 100m
        });
        await _atelier.Contexte.SaveChangesAsync();

        var mouvement = await _atelier.Contexte.InventoryTransactions
            .OrderByDescending(t => t.Id).FirstAsync();

        mouvement.QuantityBefore.Should().Be(30m);
        mouvement.QuantityAfter.Should().Be(18m);
        (await _atelier.StockMatiereAsync(matiereId)).Should().Be(18m);
    }

    [Fact]
    public async Task Le_stock_ne_peut_pas_devenir_negatif()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Émail blanc", stockInitial: 5m);

        var action = async () => await _atelier.Inventaire.EnregistrerAsync(new MouvementStockRequete
        {
            TypeArticle = InventoryItemType.MatierePremiere,
            TypeMouvement = InventoryTransactionType.ConsommationProduction,
            MatiereId = matiereId,
            Quantite = -8m
        });

        await action.Should().ThrowAsync<RegleMetierException>()
            .WithMessage("*Stock insuffisant*");
    }

    [Fact]
    public async Task Le_message_de_stock_insuffisant_indique_le_disponible_et_le_demande()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Pigment bleu", stockInitial: 2m);

        var action = async () => await _atelier.Inventaire.EnregistrerAsync(new MouvementStockRequete
        {
            TypeArticle = InventoryItemType.MatierePremiere,
            TypeMouvement = InventoryTransactionType.ConsommationProduction,
            MatiereId = matiereId,
            Quantite = -7m
        });

        var exception = await action.Should().ThrowAsync<RegleMetierException>();
        exception.Which.Message.Should().Contain("2").And.Contain("7").And.Contain("Pigment bleu");
    }

    [Fact]
    public async Task Une_derogation_autorise_exceptionnellement_le_stock_negatif()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Plâtre", stockInitial: 1m);

        await _atelier.Inventaire.EnregistrerAsync(new MouvementStockRequete
        {
            TypeArticle = InventoryItemType.MatierePremiere,
            TypeMouvement = InventoryTransactionType.ConsommationProduction,
            MatiereId = matiereId,
            Quantite = -3m,
            AutoriserStockNegatif = true
        });
        await _atelier.Contexte.SaveChangesAsync();

        (await _atelier.StockMatiereAsync(matiereId)).Should().Be(-2m);
    }

    [Fact]
    public async Task Le_reglage_de_l_atelier_peut_autoriser_le_stock_negatif()
    {
        _atelier.Contexte.SystemSettings.Add(new SystemSetting
        {
            Key = InventaireService.CleStockNegatif, Value = "true", Category = "Stock", ValueType = "booleen"
        });
        await _atelier.Contexte.SaveChangesAsync();

        var matiereId = await _atelier.CreerMatiereAsync("Colle", stockInitial: 1m);

        await _atelier.Inventaire.EnregistrerAsync(new MouvementStockRequete
        {
            TypeArticle = InventoryItemType.MatierePremiere,
            TypeMouvement = InventoryTransactionType.ConsommationProduction,
            MatiereId = matiereId,
            Quantite = -4m
        });
        await _atelier.Contexte.SaveChangesAsync();

        (await _atelier.StockMatiereAsync(matiereId)).Should().Be(-3m);
    }

    [Fact]
    public async Task Une_quantite_nulle_est_refusee()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Or décoratif");

        var action = async () => await _atelier.Inventaire.EnregistrerAsync(new MouvementStockRequete
        {
            TypeArticle = InventoryItemType.MatierePremiere,
            TypeMouvement = InventoryTransactionType.Ajustement,
            MatiereId = matiereId,
            Quantite = 0m
        });

        await action.Should().ThrowAsync<RegleMetierException>();
    }

    [Fact]
    public async Task Le_cout_moyen_est_recalcule_au_prorata_des_quantites()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile blanche", stockInitial: 10m, prix: 100m);

        // 10 kg à 100 DA puis 10 kg à 200 DA donnent un coût moyen de 150 DA.
        await _atelier.Inventaire.EnregistrerAsync(new MouvementStockRequete
        {
            TypeArticle = InventoryItemType.MatierePremiere,
            TypeMouvement = InventoryTransactionType.Achat,
            MatiereId = matiereId,
            Quantite = 10m,
            CoutUnitaire = 200m
        });
        await _atelier.Contexte.SaveChangesAsync();

        var matiere = await _atelier.Contexte.Materials.AsNoTracking().FirstAsync(m => m.Id == matiereId);
        matiere.AverageCost.Should().Be(150m);
        matiere.LastPurchasePrice.Should().Be(200m);
    }

    [Fact]
    public async Task Une_regularisation_enregistre_l_ecart_constate()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Emballage", stockInitial: 100m);

        await _atelier.Inventaire.RegulariserAsync(new RegularisationRequete
        {
            TypeArticle = InventoryItemType.MatierePremiere,
            MatiereId = matiereId,
            QuantiteComptee = 94m,
            Motif = StockAdjustmentReason.Casse,
            Notes = "Six boîtes abîmées"
        });

        var regularisation = await _atelier.Contexte.StockAdjustments.AsNoTracking().FirstAsync();
        regularisation.QuantityBefore.Should().Be(100m);
        regularisation.CountedQuantity.Should().Be(94m);
        regularisation.Difference.Should().Be(-6m);
        (await _atelier.StockMatiereAsync(matiereId)).Should().Be(94m);
    }

    [Fact]
    public async Task Une_regularisation_sans_ecart_est_refusee()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Peinture", stockInitial: 20m);

        var action = async () => await _atelier.Inventaire.RegulariserAsync(new RegularisationRequete
        {
            TypeArticle = InventoryItemType.MatierePremiere,
            MatiereId = matiereId,
            QuantiteComptee = 20m,
            Motif = StockAdjustmentReason.Inventaire
        });

        await action.Should().ThrowAsync<RegleMetierException>()
            .WithMessage("*aucune régularisation*");
    }

    [Fact]
    public async Task La_liste_des_mouvements_est_filtrable_par_matiere()
    {
        var argile = await _atelier.CreerMatiereAsync("Argile", stockInitial: 50m);
        await _atelier.CreerMatiereAsync("Émail", stockInitial: 20m);

        var page = await _atelier.Inventaire.ListerAsync(new FiltreMouvementsRequete { MatiereId = argile });

        page.Total.Should().Be(1);
        page.Elements.Single().Article.Should().Be("Argile");
    }

    public void Dispose() => _atelier.Dispose();
}
