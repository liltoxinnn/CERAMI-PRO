using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Domain.Enums;
using CeramiPro.Tests.Aides;
using FluentAssertions;

namespace CeramiPro.Tests.Commercial;

/// <summary>
/// Commandes personnalisées, souvent reçues sur Instagram ou WhatsApp :
/// acompte à la commande, reste calculé tout seul, livraison une fois soldée.
/// </summary>
public class CommandeServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    public void Dispose() => _atelier.Dispose();

    [Fact]
    public async Task Une_commande_calcule_son_total_et_son_reste()
    {
        var commande = await _atelier.Commandes.CreerAsync(new CommandeRequete
        {
            ClientId = await _atelier.CreerClientAsync(),
            Titre = "Six bols émaillés",
            Canal = CanalCommande.WhatsApp,
            Quantite = 6m,
            PrixUnitaire = 1500m,
            Remise = 1000m,
            DateLimite = _atelier.Horloge.Aujourdhui.AddDays(15)
        });

        commande.Numero.Should().StartWith("CMD-");
        commande.Total.Should().Be(8000m);
        commande.Paye.Should().Be(0m);
        commande.Reste.Should().Be(8000m);
        commande.Statut.Should().Be(CustomOrderStatus.Recue);
        commande.Canal.Should().Be(CanalCommande.WhatsApp);
        commande.CanalLibelle.Should().Be("WhatsApp");
    }

    [Fact]
    public async Task Une_commande_sans_client_sans_titre_ou_sans_date_limite_est_refusee()
    {
        var clientId = await _atelier.CreerClientAsync();
        var echeance = _atelier.Horloge.Aujourdhui.AddDays(5);

        var sansClient = () => _atelier.Commandes.CreerAsync(new CommandeRequete
        {
            Titre = "Vase", PrixUnitaire = 100m, DateLimite = echeance
        });
        var sansTitre = () => _atelier.Commandes.CreerAsync(new CommandeRequete
        {
            ClientId = clientId, PrixUnitaire = 100m, DateLimite = echeance
        });
        var sansDate = () => _atelier.Commandes.CreerAsync(new CommandeRequete
        {
            ClientId = clientId, Titre = "Vase", PrixUnitaire = 100m
        });

        await sansClient.Should().ThrowAsync<RegleMetierException>();
        await sansTitre.Should().ThrowAsync<RegleMetierException>();
        await sansDate.Should().ThrowAsync<RegleMetierException>().WithMessage("*date limite*");
    }

    [Fact]
    public async Task Les_acomptes_successifs_reduisent_le_reste()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync(), prixUnitaire: 12000m);

        await _atelier.EncaisserCommandeAsync(commande.Id, 4000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 3000m);

        var apres = await _atelier.Commandes.ObtenirAsync(commande.Id);
        apres.Paye.Should().Be(7000m);
        apres.Reste.Should().Be(5000m);
    }

    [Fact]
    public async Task Un_acompte_superieur_au_reste_est_refuse()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync(), prixUnitaire: 12000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 10000m);

        var action = () => _atelier.EncaisserCommandeAsync(commande.Id, 3000m);

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*dépasse le reste*");
        (await _atelier.Commandes.ObtenirAsync(commande.Id)).Reste.Should().Be(2000m);
    }

    [Fact]
    public async Task Les_etapes_avancent_dans_l_ordre_sans_retour_en_arriere()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync());

        await _atelier.Commandes.ChangerStatutAsync(commande.Id, CustomOrderStatus.EnFabrication);
        await _atelier.Commandes.ChangerStatutAsync(commande.Id, CustomOrderStatus.Prete);

        var retour = () => _atelier.Commandes.ChangerStatutAsync(commande.Id, CustomOrderStatus.Recue);

        await retour.Should().ThrowAsync<RegleMetierException>().WithMessage("*étape suivante*");
        (await _atelier.Commandes.ObtenirAsync(commande.Id)).Statut.Should().Be(CustomOrderStatus.Prete);
    }

    [Fact]
    public async Task Une_commande_non_soldee_ne_peut_pas_etre_livree()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync(), prixUnitaire: 12000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 4000m);

        var action = () => _atelier.Commandes.ChangerStatutAsync(commande.Id, CustomOrderStatus.Livree);

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*à encaisser*");
    }

    [Fact]
    public async Task Une_commande_soldee_se_livre_et_note_la_date_de_livraison()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync(), prixUnitaire: 12000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 4000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 8000m, acompte: false);

        var livree = await _atelier.Commandes.ChangerStatutAsync(commande.Id, CustomOrderStatus.Livree);

        livree.Statut.Should().Be(CustomOrderStatus.Livree);
        livree.DateLivraison.Should().NotBeNull();
        livree.Reste.Should().Be(0m);
    }

    [Fact]
    public async Task Une_commande_livree_ne_s_annule_plus()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync(), prixUnitaire: 5000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 5000m);
        await _atelier.Commandes.ChangerStatutAsync(commande.Id, CustomOrderStatus.Livree);

        var action = () => _atelier.Commandes.AnnulerAsync(commande.Id, "Trop tard", rembourser: true);

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*livrée*");
    }

    [Fact]
    public async Task Une_commande_avec_acompte_ne_s_annule_qu_en_remboursant()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync(), prixUnitaire: 12000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 4000m);

        var sansRemboursement = () => _atelier.Commandes.AnnulerAsync(commande.Id, "Changement d'avis");
        await sansRemboursement.Should().ThrowAsync<RegleMetierException>().WithMessage("*rembourser*");

        var annulee = await _atelier.Commandes.AnnulerAsync(commande.Id, "Changement d'avis", rembourser: true);

        annulee.Statut.Should().Be(CustomOrderStatus.Annulee);
        annulee.Paye.Should().Be(0m);
    }

    [Fact]
    public async Task Une_commande_annulee_ne_recoit_plus_de_paiement()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync());
        await _atelier.Commandes.AnnulerAsync(commande.Id, "Doublon");

        var action = () => _atelier.EncaisserCommandeAsync(commande.Id, 1000m);

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*annulée*");
    }

    [Fact]
    public async Task Le_prix_ne_peut_pas_descendre_sous_ce_qui_est_deja_encaisse()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync(), prixUnitaire: 12000m);
        await _atelier.EncaisserCommandeAsync(commande.Id, 6000m);

        var action = () => _atelier.Commandes.ModifierAsync(commande.Id, new CommandeRequete
        {
            ClientId = commande.ClientId, Titre = commande.Titre, Quantite = 1m,
            PrixUnitaire = 5000m, DateLimite = commande.DateLimite
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*déjà encaissé*");
    }

    [Fact]
    public async Task Une_commande_en_retard_est_signalee()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync());

        _atelier.Horloge.Avancer(TimeSpan.FromDays(11));

        var apres = await _atelier.Commandes.ObtenirAsync(commande.Id);
        apres.EnRetard.Should().BeTrue();
        apres.JoursRestants.Should().BeNegative();
    }

    [Fact]
    public async Task Une_commande_a_livrer_aujourd_hui_n_est_pas_en_retard_a_une_heure_du_matin()
    {
        var commande = await _atelier.Commandes.CreerAsync(new CommandeRequete
        {
            ClientId = await _atelier.CreerClientAsync(), Titre = "Plat", PrixUnitaire = 3000m,
            DateLimite = new DateTime(2026, 9, 5)
        });

        // 1 h du matin à Alger le 5 septembre : 0 h en temps universel.
        _atelier.Horloge.MaintenantUtc = new DateTime(2026, 9, 5, 0, 0, 0, DateTimeKind.Utc);

        var apres = await _atelier.Commandes.ObtenirAsync(commande.Id);
        apres.EnRetard.Should().BeFalse();
        apres.JoursRestants.Should().Be(0);
        apres.DateLimite.Should().Be(new DateTime(2026, 9, 5));
    }

    [Fact]
    public async Task Les_commandes_se_filtrent_par_canal_et_par_reste_a_payer()
    {
        var clientId = await _atelier.CreerClientAsync();
        var instagram = await _atelier.CommanderAsync(clientId, prixUnitaire: 5000m, canal: CanalCommande.Instagram);
        var whatsapp = await _atelier.CommanderAsync(clientId, prixUnitaire: 5000m, canal: CanalCommande.WhatsApp);
        await _atelier.EncaisserCommandeAsync(whatsapp.Id, 5000m);

        (await _atelier.Commandes.ListerAsync(new FiltreCommandesRequete { Canal = CanalCommande.Instagram }))
            .Elements.Should().ContainSingle().Which.Id.Should().Be(instagram.Id);

        (await _atelier.Commandes.ListerAsync(new FiltreCommandesRequete { SeulementAvecReste = true }))
            .Elements.Should().ContainSingle().Which.Id.Should().Be(instagram.Id);
    }

    [Fact]
    public async Task Les_photos_et_les_notes_sont_conservees()
    {
        var commande = await _atelier.CommanderAsync(await _atelier.CreerClientAsync());

        await _atelier.Commandes.AjouterPhotoAsync(commande.Id, new PhotoCommandeRequete
        {
            Chemin = "commandes/modele.jpg", Legende = "Modèle envoyé sur Instagram"
        });
        await _atelier.Commandes.AjouterNoteAsync(commande.Id, new NoteRequete { Contenu = "Bleu plus soutenu" });

        var apres = await _atelier.Commandes.ObtenirAsync(commande.Id);
        apres.Photos.Should().ContainSingle(p => p.Legende == "Modèle envoyé sur Instagram");
        apres.NotesHistorique.Should().Contain(n => n.Contenu == "Bleu plus soutenu");

        var chemin = await _atelier.Commandes.SupprimerPhotoAsync(commande.Id, apres.Photos[0].Id);
        chemin.Should().Be("commandes/modele.jpg");
        (await _atelier.Commandes.ObtenirAsync(commande.Id)).Photos.Should().BeEmpty();
    }
}
