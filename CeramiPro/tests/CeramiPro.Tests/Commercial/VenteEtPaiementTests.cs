using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Domain.Enums;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Tests.Commercial;

public class VenteServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    [Fact]
    public async Task Une_vente_diminue_le_stock_et_emet_une_facture()
    {
        var clientId = await _atelier.CreerClientAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m,
            coutProduction: 1850m, stockInitial: 10m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 3m } }
        });

        vente.Numero.Should().StartWith("VTE-");
        vente.Total.Should().Be(10500m);
        vente.FactureNumero.Should().StartWith("FAC-");
        (await _atelier.StockProduitAsync(produitId)).Should().Be(7m);
    }

    [Fact]
    public async Task Le_benefice_est_calcule_a_partir_du_cout_de_revient()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 3500m,
            coutProduction: 1850m, stockInitial: 10m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        vente.CoutRevient.Should().Be(1850m);
        vente.Benefice.Should().Be(1650m);
    }

    [Fact]
    public async Task Vendre_plus_que_le_stock_disponible_est_refuse()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 2m);

        var action = async () => await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 5m } }
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*Stock insuffisant*");
    }

    [Fact]
    public async Task Une_remise_est_deduite_du_total()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 1000m, stockInitial: 10m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Remise = 500m,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 5m } }
        });

        vente.SousTotal.Should().Be(5000m);
        vente.Total.Should().Be(4500m);
    }

    [Fact]
    public async Task Un_reglement_immediat_est_encaisse_avec_la_vente()
    {
        var clientId = await _atelier.CreerClientAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 1000m, stockInitial: 10m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            MontantPaye = 3000m,
            ModeReglementId = _atelier.ModeReglementId,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 5m } }
        });

        var apres = await _atelier.Ventes.ObtenirAsync(vente.Id);
        apres.Paye.Should().Be(3000m);
        apres.Reste.Should().Be(2000m);
    }

    [Fact]
    public async Task Une_vente_annulee_remet_les_produits_en_stock()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 10m);
        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 4m } }
        });

        var annulee = await _atelier.Ventes.AnnulerAsync(vente.Id, "Erreur de saisie");

        annulee.Statut.Should().Be(SaleStatus.Annulee);
        (await _atelier.StockProduitAsync(produitId)).Should().Be(10m);
    }

    [Fact]
    public async Task Une_vente_deja_reglee_ne_peut_pas_etre_annulee()
    {
        var clientId = await _atelier.CreerClientAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 1000m, stockInitial: 10m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            MontantPaye = 1000m,
            ModeReglementId = _atelier.ModeReglementId,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        var action = async () => await _atelier.Ventes.AnnulerAsync(vente.Id, "Trop tard");

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*déjà été réglée*");
    }

    [Fact]
    public async Task L_annulation_d_une_vente_annule_aussi_sa_facture()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 10m);
        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        await _atelier.Ventes.AnnulerAsync(vente.Id, "Client absent");

        var facture = await _atelier.Contexte.Invoices.AsNoTracking().FirstAsync(f => f.SaleId == vente.Id);
        facture.Status.Should().Be(InvoiceStatus.Annulee);
    }

    public void Dispose() => _atelier.Dispose();
}

public class PaiementServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    [Fact]
    public async Task Un_paiement_partiel_laisse_un_reste_a_payer()
    {
        var clientId = await _atelier.CreerClientAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 45000m, stockInitial: 5m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        await _atelier.Paiements.EnregistrerAsync(new PaiementRequete
        {
            VenteId = vente.Id, Montant = 20000m, ModeReglementId = _atelier.ModeReglementId
        });

        var apres = await _atelier.Ventes.ObtenirAsync(vente.Id);
        apres.Paye.Should().Be(20000m);
        apres.Reste.Should().Be(25000m);
    }

    [Fact]
    public async Task Les_paiements_successifs_reduisent_le_reste()
    {
        var clientId = await _atelier.CreerClientAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 45000m, stockInitial: 5m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        await _atelier.Paiements.EnregistrerAsync(new PaiementRequete
        {
            VenteId = vente.Id, Montant = 20000m, ModeReglementId = _atelier.ModeReglementId
        });
        await _atelier.Paiements.EnregistrerAsync(new PaiementRequete
        {
            VenteId = vente.Id, Montant = 10000m, ModeReglementId = _atelier.ModeReglementId
        });

        (await _atelier.Ventes.ObtenirAsync(vente.Id)).Reste.Should().Be(15000m);
    }

    [Fact]
    public async Task Un_paiement_superieur_au_reste_du_est_refuse()
    {
        var clientId = await _atelier.CreerClientAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 1000m, stockInitial: 5m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        var action = async () => await _atelier.Paiements.EnregistrerAsync(new PaiementRequete
        {
            VenteId = vente.Id, Montant = 5000m, ModeReglementId = _atelier.ModeReglementId
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*dépasse le reste à payer*");
    }

    [Fact]
    public async Task Le_paiement_complet_solde_la_facture()
    {
        var clientId = await _atelier.CreerClientAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 2000m, stockInitial: 5m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        await _atelier.Paiements.EnregistrerAsync(new PaiementRequete
        {
            VenteId = vente.Id, Montant = 2000m, ModeReglementId = _atelier.ModeReglementId
        });

        var facture = await _atelier.Contexte.Invoices.AsNoTracking().FirstAsync(f => f.SaleId == vente.Id);
        facture.Status.Should().Be(InvoiceStatus.Payee);
    }

    [Fact]
    public async Task Un_paiement_annule_reste_consultable_et_corrige_le_solde()
    {
        var clientId = await _atelier.CreerClientAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase", prixVente: 2000m, stockInitial: 5m);

        var vente = await _atelier.Ventes.EnregistrerAsync(new VenteRequete
        {
            ClientId = clientId,
            Lignes = new List<LigneVenteRequete> { new() { ProduitId = produitId, Quantite = 1m } }
        });

        var paiement = await _atelier.Paiements.EnregistrerAsync(new PaiementRequete
        {
            VenteId = vente.Id, Montant = 2000m, ModeReglementId = _atelier.ModeReglementId
        });

        await _atelier.Paiements.AnnulerAsync(paiement.Id, "Chèque sans provision");

        (await _atelier.Ventes.ObtenirAsync(vente.Id)).Paye.Should().Be(0m);

        // Règle métier n°15 : l'écriture est conservée, seulement marquée supprimée.
        var conserve = await _atelier.Contexte.Payments.IgnoreQueryFilters()
            .FirstAsync(p => p.Id == paiement.Id);
        conserve.IsDeleted.Should().BeTrue();
        conserve.Notes.Should().Contain("Chèque sans provision");
    }

    public void Dispose() => _atelier.Dispose();
}

public class CommandeServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    private async Task<CommandeDto> CreerCommandeAsync(decimal prix = 15000m, decimal quantite = 3m)
    {
        var clientId = await _atelier.CreerClientAsync();

        return await _atelier.Commandes.CreerAsync(new CommandeRequete
        {
            ClientId = clientId,
            Titre = "Décoration murale personnalisée",
            Largeur = 120m,
            Hauteur = 80m,
            Couleurs = "Blanc + Or",
            Quantite = quantite,
            PrixUnitaire = prix,
            DateLimite = _atelier.Horloge.MaintenantUtc.AddDays(20)
        });
    }

    [Fact]
    public async Task Une_commande_calcule_son_total_et_son_reste()
    {
        var commande = await CreerCommandeAsync();

        commande.Numero.Should().StartWith("CMD-");
        commande.Total.Should().Be(45000m);
        commande.Reste.Should().Be(45000m);
        commande.Statut.Should().Be(CustomOrderStatus.Commande);
    }

    [Fact]
    public async Task Une_commande_sans_date_limite_est_refusee()
    {
        var clientId = await _atelier.CreerClientAsync();

        var action = async () => await _atelier.Commandes.CreerAsync(new CommandeRequete
        {
            ClientId = clientId, Titre = "Pièce unique", Quantite = 1m, PrixUnitaire = 1000m
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*date limite*");
    }

    [Fact]
    public async Task Un_acompte_reduit_le_reste_a_payer()
    {
        var commande = await CreerCommandeAsync();

        await _atelier.Paiements.EnregistrerAsync(new PaiementRequete
        {
            CommandeId = commande.Id, Montant = 20000m,
            ModeReglementId = _atelier.ModeReglementId, Acompte = true
        });

        var apres = await _atelier.Commandes.ObtenirAsync(commande.Id);
        apres.Paye.Should().Be(20000m);
        apres.Reste.Should().Be(25000m);
    }

    [Fact]
    public async Task Une_commande_en_retard_est_signalee()
    {
        var commande = await CreerCommandeAsync();
        _atelier.Horloge.Avancer(TimeSpan.FromDays(25));

        var apres = await _atelier.Commandes.ObtenirAsync(commande.Id);

        apres.EnRetard.Should().BeTrue();
        apres.JoursRestants.Should().BeNegative();
    }

    [Fact]
    public async Task Les_etapes_de_la_commande_avancent_dans_l_ordre()
    {
        var commande = await CreerCommandeAsync();

        await _atelier.Commandes.ChangerStatutAsync(commande.Id, CustomOrderStatus.Conception);
        var apres = await _atelier.Commandes.ChangerStatutAsync(commande.Id, CustomOrderStatus.ValidationClient);

        apres.Statut.Should().Be(CustomOrderStatus.ValidationClient);

        var action = async () => await _atelier.Commandes.ChangerStatutAsync(
            commande.Id, CustomOrderStatus.Conception);

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*étape suivante*");
    }

    [Fact]
    public async Task Une_commande_non_soldee_ne_peut_pas_etre_livree()
    {
        var commande = await CreerCommandeAsync();

        foreach (var etape in new[]
                 {
                     CustomOrderStatus.Conception, CustomOrderStatus.ValidationClient,
                     CustomOrderStatus.Production, CustomOrderStatus.Cuisson,
                     CustomOrderStatus.Decoration, CustomOrderStatus.ControleQualite,
                     CustomOrderStatus.Pret
                 })
        {
            await _atelier.Commandes.ChangerStatutAsync(commande.Id, etape);
        }

        var action = async () => await _atelier.Commandes.ChangerStatutAsync(
            commande.Id, CustomOrderStatus.Livre);

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*à encaisser*");
    }

    [Fact]
    public async Task Les_photos_et_les_notes_sont_conservees()
    {
        var commande = await CreerCommandeAsync();

        await _atelier.Commandes.AjouterPhotoAsync(commande.Id, new PhotoCommandeRequete
        {
            Chemin = "/fichiers/2026-03/croquis.jpg",
            Type = CustomOrderImageKind.Croquis,
            Legende = "Croquis validé par le client"
        });

        var apres = await _atelier.Commandes.AjouterNoteAsync(commande.Id, new NoteRequete
        {
            Contenu = "Le client souhaite un or plus clair."
        });

        apres.Photos.Should().ContainSingle().Which.TypeLibelle.Should().Be("Croquis");
        apres.NotesHistorique.Should().ContainSingle()
            .Which.Contenu.Should().Contain("or plus clair");
    }

    [Fact]
    public async Task Une_commande_avec_acompte_ne_peut_pas_etre_annulee()
    {
        var commande = await CreerCommandeAsync();

        await _atelier.Paiements.EnregistrerAsync(new PaiementRequete
        {
            CommandeId = commande.Id, Montant = 5000m,
            ModeReglementId = _atelier.ModeReglementId, Acompte = true
        });

        var action = async () => await _atelier.Commandes.AnnulerAsync(commande.Id, "Client injoignable");

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*acompte*");
    }

    [Fact]
    public async Task Une_facture_peut_etre_emise_pour_une_commande()
    {
        var commande = await CreerCommandeAsync();

        var facture = await _atelier.Factures.EmettrePourCommandeAsync(new FactureCommandeRequete
        {
            CommandeId = commande.Id
        });

        facture.Numero.Should().StartWith("FAC-");
        facture.Total.Should().Be(45000m);
        facture.CommandeNumero.Should().Be(commande.Numero);
    }

    [Fact]
    public async Task Une_commande_ne_peut_pas_avoir_deux_factures()
    {
        var commande = await CreerCommandeAsync();
        await _atelier.Factures.EmettrePourCommandeAsync(new FactureCommandeRequete { CommandeId = commande.Id });

        var action = async () => await _atelier.Factures.EmettrePourCommandeAsync(
            new FactureCommandeRequete { CommandeId = commande.Id });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*possède déjà la facture*");
    }

    [Fact]
    public async Task La_dette_du_client_reflete_ses_commandes_et_ses_paiements()
    {
        var commande = await CreerCommandeAsync();

        await _atelier.Paiements.EnregistrerAsync(new PaiementRequete
        {
            CommandeId = commande.Id, Montant = 20000m,
            ModeReglementId = _atelier.ModeReglementId, Acompte = true
        });

        var dettes = await _atelier.Clients.ListerDettesAsync();

        dettes.Should().ContainSingle();
        dettes[0].TotalDu.Should().Be(45000m);
        dettes[0].TotalPaye.Should().Be(20000m);
        dettes[0].Reste.Should().Be(25000m);
    }

    public void Dispose() => _atelier.Dispose();
}
