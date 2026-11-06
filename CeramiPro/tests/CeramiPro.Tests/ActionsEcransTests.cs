using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.ViewModels.Ecrans;
using CeramiPro.Presentation.ViewModels.Formulaires;
using CeramiPro.Tests.Aides;
using FluentAssertions;

namespace CeramiPro.Tests;

/// <summary>
/// Les gestes du métier lancés depuis les écrans : encaisser un acompte,
/// faire avancer une commande, annuler une vente, enregistrer une casse.
///
/// Ces actions passent par les vrais services : ce sont elles qui déplacent
/// réellement le stock et l'argent, et elles sont donc vérifiées de bout en
/// bout, depuis le bouton jusqu'à la base.
/// </summary>
public class ActionsEcransTests : IDisposable
{
    private readonly InterfaceDeTest _ui = new();

    private AtelierDeTest Atelier => _ui.Atelier;

    public void Dispose() => _ui.Dispose();

    // ----------------------------------------------------------- Commandes

    [Fact]
    public async Task L_ecran_des_commandes_propose_les_gestes_du_metier()
    {
        var ecran = await _ui.OuvrirAsync<CommandesVueModele>();

        ecran.Actions.Select(a => a.Libelle).Should().Contain(new[]
        {
            "💰  Encaisser un acompte", "Étape suivante", "Marquer comme livrée", "Annuler la commande"
        });

        // Annuler se distingue : c'est la seule action qui défait du travail.
        ecran.Actions.Single(a => a.Destructive).Libelle.Should().Be("Annuler la commande");
    }

    [Fact]
    public async Task Une_action_sans_ligne_choisie_le_dit_plutot_que_d_echouer()
    {
        var ecran = await _ui.OuvrirAsync<CommandesVueModele>();

        await ecran.AvancerCommand.ExecuteAsync(null);

        _ui.Dialogue.Messages.Should().ContainSingle(m => m.Niveau == "avertissement");
        _ui.Dialogue.Messages[0].Message.Should().Contain("Choisissez d'abord");
    }

    [Fact]
    public async Task Encaisser_un_acompte_met_le_reste_a_payer_a_jour()
    {
        var commande = await Atelier.CommanderAsync(await Atelier.CreerClientAsync(), prixUnitaire: 12000m);

        var ecran = await _ui.OuvrirAsync<CommandesVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(c => c.Id == commande.Id);

        // Le formulaire s'ouvre sur la commande, avec tout le reste proposé :
        // le client verse ici un acompte de 4 000 DA.
        _ui.Formulaires.PendantLaSaisie = formulaire =>
        {
            var paiement = (PaiementFormulaireVueModele)formulaire;
            paiement.Requete.Document.Should().Be(ReglementRequete.CodeCommande(commande.Id));
            paiement.Requete.Montant.Should().Be(12000m);
            paiement.Requete.Montant = 4000m;
        };

        await ecran.EncaisserCommand.ExecuteAsync(null);

        _ui.Formulaires.DerniereErreur.Should().BeNull();

        var apres = await Atelier.Commandes.ObtenirAsync(commande.Id);
        apres.Paye.Should().Be(4000m);
        apres.Reste.Should().Be(8000m);

        // Le tableau est rechargé : la ligne affiche déjà le nouveau reste.
        ecran.Elements.Single(c => c.Id == commande.Id).Reste.Should().Be(8000m);

        var paiements = await Atelier.Paiements.ListerAsync(new FiltrePaiementsRequete { CommandeId = commande.Id });
        paiements.Elements.Should().ContainSingle(p => p.Acompte);
    }

    [Fact]
    public async Task Une_commande_avance_etape_par_etape()
    {
        var commande = await Atelier.CommanderAsync(await Atelier.CreerClientAsync());

        var ecran = await _ui.OuvrirAsync<CommandesVueModele>();

        ecran.ElementSelectionne = ecran.Elements.Single(c => c.Id == commande.Id);
        await ecran.AvancerCommand.ExecuteAsync(null);
        (await Atelier.Commandes.ObtenirAsync(commande.Id)).Statut.Should().Be(CustomOrderStatus.EnFabrication);

        ecran.ElementSelectionne = ecran.Elements.Single(c => c.Id == commande.Id);
        await ecran.AvancerCommand.ExecuteAsync(null);
        (await Atelier.Commandes.ObtenirAsync(commande.Id)).Statut.Should().Be(CustomOrderStatus.Prete);
    }

    [Fact]
    public async Task Une_commande_non_soldee_ne_peut_pas_etre_livree()
    {
        var commande = await Atelier.CommanderAsync(await Atelier.CreerClientAsync(), prixUnitaire: 12000m);
        await Atelier.EncaisserCommandeAsync(commande.Id, 4000m);

        var ecran = await _ui.OuvrirAsync<CommandesVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(c => c.Id == commande.Id);

        await ecran.LivrerCommand.ExecuteAsync(null);

        // Le message vient du métier et parle français : pas de trace technique.
        ecran.MessageErreur.Should().Contain("8").And.Contain("000");
        _ui.Dialogue.Messages.Should().Contain(m => m.Niveau == "erreur");
        (await Atelier.Commandes.ObtenirAsync(commande.Id)).Statut.Should().Be(CustomOrderStatus.Recue);
    }

    [Fact]
    public async Task Une_commande_soldee_se_livre()
    {
        var commande = await Atelier.CommanderAsync(await Atelier.CreerClientAsync(), prixUnitaire: 12000m);
        await Atelier.EncaisserCommandeAsync(commande.Id, 12000m);

        var ecran = await _ui.OuvrirAsync<CommandesVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(c => c.Id == commande.Id);

        await ecran.LivrerCommand.ExecuteAsync(null);

        ecran.MessageErreur.Should().BeNull();
        (await Atelier.Commandes.ObtenirAsync(commande.Id)).Statut.Should().Be(CustomOrderStatus.Livree);
    }

    [Fact]
    public async Task Annuler_une_commande_avec_acompte_rembourse_le_client()
    {
        var commande = await Atelier.CommanderAsync(await Atelier.CreerClientAsync(), prixUnitaire: 12000m);
        await Atelier.EncaisserCommandeAsync(commande.Id, 4000m);

        var ecran = await _ui.OuvrirAsync<CommandesVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(c => c.Id == commande.Id);

        _ui.Dialogue.ReponseDemande = "Le client a changé d'avis";
        await ecran.AnnulerCommand.ExecuteAsync(null);

        ecran.MessageErreur.Should().BeNull();
        _ui.Dialogue.Messages.Should().Contain(m => m.Niveau == "question" && m.Message.Contains("rembourser"));

        var apres = await Atelier.Commandes.ObtenirAsync(commande.Id);
        apres.Statut.Should().Be(CustomOrderStatus.Annulee);
        apres.Paye.Should().Be(0m);

        // L'acompte ne compte plus comme encaissé : il a été rendu.
        (await Atelier.Paiements.ListerAsync(new FiltrePaiementsRequete { CommandeId = commande.Id }))
            .Elements.Should().BeEmpty();
    }

    [Fact]
    public async Task Renoncer_a_saisir_le_motif_n_annule_rien()
    {
        var commande = await Atelier.CommanderAsync(await Atelier.CreerClientAsync());

        var ecran = await _ui.OuvrirAsync<CommandesVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(c => c.Id == commande.Id);

        _ui.Dialogue.ReponseDemande = null;
        await ecran.AnnulerCommand.ExecuteAsync(null);

        (await Atelier.Commandes.ObtenirAsync(commande.Id)).Statut.Should().Be(CustomOrderStatus.Recue);
    }

    [Fact]
    public async Task Le_filtre_des_canaux_ne_montre_que_les_commandes_whatsapp()
    {
        var clientId = await Atelier.CreerClientAsync();
        await Atelier.CommanderAsync(clientId, canal: CanalCommande.Instagram);
        var whatsapp = await Atelier.CommanderAsync(clientId, canal: CanalCommande.WhatsApp);

        var ecran = await _ui.OuvrirAsync<CommandesVueModele>();
        ecran.Elements.Should().HaveCount(2);

        var canal = (Presentation.ViewModels.FiltreChoix)ecran.Filtres[1];
        canal.Valeur = (int)CanalCommande.WhatsApp;
        await ecran.ChargerAsync();

        ecran.Elements.Should().ContainSingle().Which.Id.Should().Be(whatsapp.Id);
    }

    // ------------------------------------------------------------- Ventes

    [Fact]
    public async Task Annuler_une_vente_remet_les_produits_en_stock()
    {
        var produitId = await Atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);
        var vente = await Atelier.VendreAsync(produitId, quantite: 3m, prix: 4200m,
            clientId: await Atelier.CreerClientAsync());

        (await Atelier.StockProduitAsync(produitId)).Should().Be(7m);

        var ecran = await _ui.OuvrirAsync<VentesVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(v => v.Id == vente.Id);

        await ecran.AnnulerCommand.ExecuteAsync(null);

        ecran.MessageErreur.Should().BeNull();
        (await Atelier.StockProduitAsync(produitId)).Should().Be(10m);
        (await Atelier.Ventes.ObtenirAsync(vente.Id)).Statut.Should().Be(SaleStatus.Annulee);
    }

    [Fact]
    public async Task Annuler_une_vente_payee_rembourse_et_remet_en_stock()
    {
        var produitId = await Atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);
        var vente = await Atelier.VendreAsync(produitId, prix: 4200m, paye: 4200m);

        var ecran = await _ui.OuvrirAsync<VentesVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(v => v.Id == vente.Id);

        await ecran.AnnulerCommand.ExecuteAsync(null);

        ecran.MessageErreur.Should().BeNull();
        (await Atelier.StockProduitAsync(produitId)).Should().Be(10m);
        (await Atelier.Paiements.ListerAsync(new FiltrePaiementsRequete { VenteId = vente.Id }))
            .Elements.Should().BeEmpty();
    }

    [Fact]
    public async Task Encaisser_le_reste_d_une_vente_a_credit_la_solde()
    {
        var clientId = await Atelier.CreerClientAsync();
        var produitId = await Atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);
        var vente = await Atelier.VendreAsync(produitId, prix: 4200m, paye: 1200m, clientId: clientId);

        var ecran = await _ui.OuvrirAsync<VentesVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(v => v.Id == vente.Id);

        _ui.Formulaires.PendantLaSaisie = _ => { };
        await ecran.EncaisserCommand.ExecuteAsync(null);

        _ui.Formulaires.DerniereErreur.Should().BeNull();
        (await Atelier.Ventes.ObtenirAsync(vente.Id)).Reste.Should().Be(0m);
    }

    [Fact]
    public async Task On_ne_rembourse_pas_plus_que_le_reste_du()
    {
        var clientId = await Atelier.CreerClientAsync();
        var produitId = await Atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);
        var vente = await Atelier.VendreAsync(produitId, prix: 4200m, paye: 1200m, clientId: clientId);

        var ecran = await _ui.OuvrirAsync<VentesVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(v => v.Id == vente.Id);

        _ui.Formulaires.PendantLaSaisie = f => ((PaiementFormulaireVueModele)f).Requete.Montant = 5000m;
        await ecran.EncaisserCommand.ExecuteAsync(null);

        _ui.Formulaires.DerniereErreur.Should().NotBeNullOrWhiteSpace();
        (await Atelier.Ventes.ObtenirAsync(vente.Id)).Reste.Should().Be(3000m);
    }

    // -------------------------------------------------------------- Stock

    [Fact]
    public async Task Une_entree_en_stock_augmente_la_quantite_et_laisse_une_trace()
    {
        var produitId = await Atelier.CreerProduitAsync("Bol vert", stockInitial: 2m);

        var ecran = await _ui.OuvrirAsync<ProduitsStockVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(p => p.Id == produitId);

        _ui.Formulaires.PendantLaSaisie = f =>
        {
            var mouvement = (MouvementStockFormulaireVueModele)f;
            mouvement.Requete.ProduitId.Should().Be(produitId);
            mouvement.Requete.Quantite = 6m;
            mouvement.Requete.Notes = "Sortie du four du 31 août";
        };

        await ecran.EntreeCommand.ExecuteAsync(null);

        _ui.Formulaires.DerniereErreur.Should().BeNull();
        (await Atelier.StockProduitAsync(produitId)).Should().Be(8m);

        var historique = await Atelier.Inventaire.ListerAsync(new FiltreMouvementsRequete { ProduitId = produitId });
        historique.Elements.Should().Contain(m => m.Quantite == 6m && m.Notes == "Sortie du four du 31 août");
    }

    [Fact]
    public async Task Une_casse_sans_precision_est_refusee()
    {
        var produitId = await Atelier.CreerProduitAsync("Bol vert", stockInitial: 5m);

        var ecran = await _ui.OuvrirAsync<ProduitsStockVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(p => p.Id == produitId);

        _ui.Formulaires.PendantLaSaisie = f => ((MouvementStockFormulaireVueModele)f).Requete.Quantite = 1m;
        await ecran.CasseCommand.ExecuteAsync(null);

        _ui.Formulaires.DerniereErreur.Should().NotBeNullOrWhiteSpace();
        (await Atelier.StockProduitAsync(produitId)).Should().Be(5m);
    }

    [Fact]
    public async Task Une_casse_diminue_le_stock()
    {
        var produitId = await Atelier.CreerProduitAsync("Bol vert", stockInitial: 5m);

        var ecran = await _ui.OuvrirAsync<ProduitsStockVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(p => p.Id == produitId);

        _ui.Formulaires.PendantLaSaisie = f =>
        {
            var mouvement = (MouvementStockFormulaireVueModele)f;
            mouvement.Requete.Quantite = 2m;
            mouvement.Requete.Notes = "Fêlés au déballage";
        };
        await ecran.CasseCommand.ExecuteAsync(null);

        _ui.Formulaires.DerniereErreur.Should().BeNull();
        (await Atelier.StockProduitAsync(produitId)).Should().Be(3m);
    }

    [Fact]
    public async Task Corriger_apres_comptage_enregistre_l_ecart()
    {
        var produitId = await Atelier.CreerProduitAsync("Bol vert", stockInitial: 5m);

        var ecran = await _ui.OuvrirAsync<ProduitsStockVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(p => p.Id == produitId);

        _ui.Formulaires.PendantLaSaisie = f =>
        {
            var correction = (RegularisationFormulaireVueModele)f;
            correction.Requete.QuantiteComptee.Should().Be(5m, "le stock actuel est proposé");
            correction.Requete.QuantiteComptee = 4m;
            correction.Requete.Motif = "Erreur de comptage";
        };
        await ecran.CorrigerCommand.ExecuteAsync(null);

        _ui.Formulaires.DerniereErreur.Should().BeNull();
        (await Atelier.StockProduitAsync(produitId)).Should().Be(4m);
    }

    [Fact]
    public async Task Une_piece_unique_ne_peut_pas_recevoir_un_second_exemplaire()
    {
        var piece = await Atelier.CreerProduitAsync("Jarre signée", stockInitial: 1m, pieceUnique: true);

        var ecran = await _ui.OuvrirAsync<ProduitsStockVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(p => p.Id == piece);

        _ui.Formulaires.PendantLaSaisie = f => ((MouvementStockFormulaireVueModele)f).Requete.Quantite = 1m;
        await ecran.EntreeCommand.ExecuteAsync(null);

        _ui.Formulaires.DerniereErreur.Should().Contain("pièce unique");
        (await Atelier.StockProduitAsync(piece)).Should().Be(1m);
    }

    // ----------------------------------------------------------- Dépenses

    [Fact]
    public async Task Supprimer_une_depense_demande_un_motif()
    {
        var depenseId = await Atelier.CreerDepenseAsync(2500m);

        var ecran = await _ui.OuvrirAsync<DepensesVueModele>();
        ecran.ElementSelectionne = ecran.Elements.Single(d => d.Id == depenseId);

        await ecran.SupprimerDepenseCommand.ExecuteAsync(null);

        ecran.MessageErreur.Should().BeNull();
        ecran.Elements.Should().BeEmpty();
        (await Atelier.Depenses.ListerAsync(new Application.DTOs.Finances.FiltreDepensesRequete()))
            .Total.Should().Be(0);
    }
}
