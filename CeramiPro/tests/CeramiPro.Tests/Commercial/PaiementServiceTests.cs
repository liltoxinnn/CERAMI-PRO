using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Tests.Commercial;

public class PaiementServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    public void Dispose() => _atelier.Dispose();

    private async Task<VenteDto> VenteACreditAsync(decimal total = 5000m, decimal paye = 0m)
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: total, stockInitial: 10m);
        return await _atelier.VendreAsync(produitId, prix: total, paye: paye, clientId: await _atelier.CreerClientAsync());
    }

    private Task<PaiementDto> EncaisserVenteAsync(int venteId, decimal montant)
        => _atelier.Paiements.EnregistrerAsync(new PaiementRequete
        {
            VenteId = venteId, Montant = montant, ModeReglementId = _atelier.ModeReglementId
        });

    [Fact]
    public async Task Les_paiements_successifs_reduisent_le_reste_d_une_vente()
    {
        var vente = await VenteACreditAsync(5000m, paye: 1000m);

        await EncaisserVenteAsync(vente.Id, 1500m);
        var paiement = await EncaisserVenteAsync(vente.Id, 2500m);

        paiement.Numero.Should().StartWith("PAI-");
        var apres = await _atelier.Ventes.ObtenirAsync(vente.Id);
        apres.Paye.Should().Be(5000m);
        apres.Reste.Should().Be(0m);
    }

    [Fact]
    public async Task Un_paiement_superieur_au_reste_du_est_refuse()
    {
        var vente = await VenteACreditAsync(5000m, paye: 4000m);

        var action = () => EncaisserVenteAsync(vente.Id, 1500m);

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*dépasse le reste*");
    }

    [Fact]
    public async Task Un_paiement_nul_ou_sans_document_est_refuse()
    {
        var vente = await VenteACreditAsync();

        var nul = () => EncaisserVenteAsync(vente.Id, 0m);
        var sansDocument = () => _atelier.Paiements.EnregistrerAsync(new PaiementRequete
        {
            Montant = 100m, ModeReglementId = _atelier.ModeReglementId
        });

        await nul.Should().ThrowAsync<RegleMetierException>();
        await sansDocument.Should().ThrowAsync<RegleMetierException>();
    }

    [Fact]
    public async Task Un_paiement_annule_reste_consultable_et_corrige_le_solde()
    {
        var vente = await VenteACreditAsync(5000m);
        var paiement = await EncaisserVenteAsync(vente.Id, 2000m);

        await _atelier.Paiements.AnnulerAsync(paiement.Id, "Chèque sans provision");

        (await _atelier.Ventes.ObtenirAsync(vente.Id)).Reste.Should().Be(5000m);

        // Il disparaît des listes, pas de la base : l'annulation est tracée.
        (await _atelier.Paiements.ListerAsync(new FiltrePaiementsRequete { VenteId = vente.Id }))
            .Elements.Should().BeEmpty();

        var archive = await _atelier.Contexte.Payments.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(p => p.Id == paiement.Id);
        archive.IsDeleted.Should().BeTrue();
        archive.DeletionReason.Should().Be("Chèque sans provision");
    }

    [Fact]
    public async Task Annuler_un_paiement_exige_un_motif()
    {
        var vente = await VenteACreditAsync(5000m);
        var paiement = await EncaisserVenteAsync(vente.Id, 2000m);

        var action = () => _atelier.Paiements.AnnulerAsync(paiement.Id, " ");

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*motif*");
    }

    [Fact]
    public async Task Le_paiement_d_une_commande_est_rattache_a_son_client()
    {
        var clientId = await _atelier.CreerClientAsync();
        var commande = await _atelier.CommanderAsync(clientId);

        var paiement = await _atelier.EncaisserCommandeAsync(commande.Id, 3000m);

        paiement.ClientId.Should().Be(clientId);
        paiement.CommandeNumero.Should().Be(commande.Numero);
        paiement.Acompte.Should().BeTrue();
    }

    [Fact]
    public async Task L_encaisse_du_jour_compte_les_ventes_et_les_acomptes()
    {
        await VenteACreditAsync(5000m, paye: 1000m);
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync());
        await _atelier.EncaisserCommandeAsync(commande.Id, 3000m);

        var tableau = await _atelier.TableauDeBord.ObtenirAsync();

        tableau.Aujourdhui.Encaisse.Should().Be(4000m);
    }
}
