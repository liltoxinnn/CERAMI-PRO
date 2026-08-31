using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Stock;
using CeramicWorkshop.Domain.Enums;
using CeramicWorkshop.UnitTests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.UnitTests.Stock;

public class AchatServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    private async Task<AchatDto> CreerAchatAsync(int matiereId, decimal quantite = 30m, decimal prix = 120m)
        => await _atelier.Achats.CreerAsync(new AchatRequete
        {
            FournisseurId = _atelier.FournisseurId,
            Lignes = new List<LigneAchatRequete>
            {
                new()
                {
                    MatiereId = matiereId,
                    UniteId = _atelier.UniteKiloId,
                    Quantite = quantite,
                    PrixUnitaire = prix
                }
            }
        });

    [Fact]
    public async Task Un_achat_est_cree_en_brouillon_avec_un_numero()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile");

        var achat = await CreerAchatAsync(matiereId);

        achat.Statut.Should().Be(PurchaseStatus.Brouillon);
        achat.Numero.Should().StartWith("ACH-");
        achat.Total.Should().Be(3600m);
    }

    [Fact]
    public async Task La_saisie_d_un_achat_ne_touche_pas_au_stock()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile", stockInitial: 10m);

        await CreerAchatAsync(matiereId);

        (await _atelier.StockMatiereAsync(matiereId)).Should().Be(10m);
    }

    [Fact]
    public async Task La_reception_augmente_le_stock_et_cree_un_lot()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile", stockInitial: 10m);
        var achat = await CreerAchatAsync(matiereId, quantite: 30m, prix: 120m);
        await _atelier.Achats.ConfirmerAsync(achat.Id);

        var recu = await _atelier.Achats.ReceptionnerAsync(achat.Id, new ReceptionAchatRequete
        {
            Lignes = new List<LigneReceptionRequete>
            {
                new() { LigneAchatId = achat.Lignes[0].Id, QuantiteRecue = 30m }
            }
        });

        recu.Statut.Should().Be(PurchaseStatus.Recu);
        (await _atelier.StockMatiereAsync(matiereId)).Should().Be(40m);

        var lot = await _atelier.Contexte.MaterialBatches.AsNoTracking().SingleAsync();
        lot.Quantity.Should().Be(30m);
        lot.UnitCost.Should().Be(120m);
        lot.BatchNumber.Should().StartWith("LOT-");
    }

    [Fact]
    public async Task Une_reception_partielle_laisse_l_achat_ouvert()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile");
        var achat = await CreerAchatAsync(matiereId, quantite: 30m);
        await _atelier.Achats.ConfirmerAsync(achat.Id);

        var recu = await _atelier.Achats.ReceptionnerAsync(achat.Id, new ReceptionAchatRequete
        {
            Lignes = new List<LigneReceptionRequete>
            {
                new() { LigneAchatId = achat.Lignes[0].Id, QuantiteRecue = 12m }
            }
        });

        recu.Statut.Should().Be(PurchaseStatus.PartiellementRecu);
        recu.Lignes[0].QuantiteRecue.Should().Be(12m);
        (await _atelier.StockMatiereAsync(matiereId)).Should().Be(12m);
    }

    [Fact]
    public async Task Recevoir_plus_que_commande_est_refuse()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile");
        var achat = await CreerAchatAsync(matiereId, quantite: 30m);
        await _atelier.Achats.ConfirmerAsync(achat.Id);

        var action = async () => await _atelier.Achats.ReceptionnerAsync(achat.Id, new ReceptionAchatRequete
        {
            Lignes = new List<LigneReceptionRequete>
            {
                new() { LigneAchatId = achat.Lignes[0].Id, QuantiteRecue = 45m }
            }
        });

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*dépasse la quantité commandée*");
    }

    [Fact]
    public async Task Un_achat_doit_etre_confirme_avant_reception()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile");
        var achat = await CreerAchatAsync(matiereId);

        var action = async () => await _atelier.Achats.ReceptionnerAsync(achat.Id, new ReceptionAchatRequete
        {
            Lignes = new List<LigneReceptionRequete>
            {
                new() { LigneAchatId = achat.Lignes[0].Id, QuantiteRecue = 5m }
            }
        });

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Confirmez l'achat*");
    }

    [Fact]
    public async Task Un_achat_confirme_ne_peut_plus_etre_modifie()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile");
        var achat = await CreerAchatAsync(matiereId);
        await _atelier.Achats.ConfirmerAsync(achat.Id);

        var action = async () => await _atelier.Achats.ModifierAsync(achat.Id, new AchatRequete
        {
            FournisseurId = _atelier.FournisseurId,
            Lignes = new List<LigneAchatRequete>
            {
                new() { MatiereId = matiereId, UniteId = _atelier.UniteKiloId, Quantite = 5m, PrixUnitaire = 10m }
            }
        });

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*brouillon*");
    }

    [Fact]
    public async Task L_annulation_ressort_du_stock_les_quantites_recues()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile", stockInitial: 10m);
        var achat = await CreerAchatAsync(matiereId, quantite: 30m);
        await _atelier.Achats.ConfirmerAsync(achat.Id);
        await _atelier.Achats.ReceptionnerAsync(achat.Id, new ReceptionAchatRequete
        {
            Lignes = new List<LigneReceptionRequete>
            {
                new() { LigneAchatId = achat.Lignes[0].Id, QuantiteRecue = 30m }
            }
        });

        var annule = await _atelier.Achats.AnnulerAsync(achat.Id, "Marchandise non conforme");

        annule.Statut.Should().Be(PurchaseStatus.Annule);
        (await _atelier.StockMatiereAsync(matiereId)).Should().Be(10m);
    }

    [Fact]
    public async Task L_annulation_cree_un_mouvement_inverse_relie_a_l_original()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile");
        var achat = await CreerAchatAsync(matiereId, quantite: 20m);
        await _atelier.Achats.ConfirmerAsync(achat.Id);
        await _atelier.Achats.ReceptionnerAsync(achat.Id, new ReceptionAchatRequete
        {
            Lignes = new List<LigneReceptionRequete>
            {
                new() { LigneAchatId = achat.Lignes[0].Id, QuantiteRecue = 20m }
            }
        });

        await _atelier.Achats.AnnulerAsync(achat.Id, "Erreur de commande");

        var inverse = await _atelier.Contexte.InventoryTransactions.AsNoTracking()
            .FirstAsync(t => t.TransactionType == InventoryTransactionType.Annulation);

        inverse.Quantity.Should().Be(-20m);
        inverse.ReversedTransactionId.Should().NotBeNull();
    }

    [Fact]
    public async Task Une_annulation_sans_motif_est_refusee()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile");
        var achat = await CreerAchatAsync(matiereId);

        var action = async () => await _atelier.Achats.AnnulerAsync(achat.Id, "   ");

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*motif*");
    }

    [Fact]
    public async Task Un_achat_deja_regle_ne_peut_pas_etre_annule()
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile");
        var achat = await CreerAchatAsync(matiereId, quantite: 10m, prix: 100m);
        await _atelier.Achats.ConfirmerAsync(achat.Id);

        await _atelier.Fournisseurs.EnregistrerReglementAsync(new ReglementFournisseurRequete
        {
            FournisseurId = _atelier.FournisseurId,
            AchatId = achat.Id,
            Montant = 400m,
            ModeReglementId = _atelier.ModeReglementId
        });

        var action = async () => await _atelier.Achats.AnnulerAsync(achat.Id, "Changement d'avis");

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*déjà été réglé*");
    }

    [Fact]
    public async Task Un_achat_sans_ligne_est_refuse()
    {
        var action = async () => await _atelier.Achats.CreerAsync(new AchatRequete
        {
            FournisseurId = _atelier.FournisseurId,
            Lignes = new List<LigneAchatRequete>()
        });

        await action.Should().ThrowAsync<BusinessRuleException>().WithMessage("*au moins une matière*");
    }

    public void Dispose() => _atelier.Dispose();
}
