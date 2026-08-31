using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Ecrans;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Tests;

/// <summary>
/// Les gestes du métier lancés depuis les écrans de liste : réceptionner un
/// achat, défourner une cuisson, annuler une vente.
///
/// Ces actions passent par les vrais services : ce sont elles qui déplacent
/// réellement le stock, et elles doivent donc être vérifiées de bout en bout.
/// </summary>
public class ActionsEcransTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();
    private readonly DialogueFactice _dialogue = new();

    public ActionsEcransTests() => _atelier.AccorderTousLesDroits();

    public void Dispose() => _atelier.Dispose();

    private OutilsListe Outils() => new(
        new FormulaireFactice(), _dialogue, new FichierFactice(),
        null!, new ServiceCollection().BuildServiceProvider());

    // ------------------------------------------------------------- Achats

    private async Task<int> CreerAchatAsync(decimal quantite = 25m)
    {
        var matiereId = await _atelier.CreerMatiereAsync("Argile blanche", stockInitial: 0m, prix: 200m);

        var achat = await _atelier.Achats.CreerAsync(new AchatRequete
        {
            FournisseurId = _atelier.FournisseurId,
            Lignes = new List<LigneAchatRequete>
            {
                new()
                {
                    MatiereId = matiereId,
                    UniteId = _atelier.UniteKiloId,
                    Quantite = quantite,
                    PrixUnitaire = 200m
                }
            }
        });

        return achat.Id;
    }

    private AchatsVueModele Achats() => new(_atelier.Achats, new ServiceLangue(), Outils());

    [Fact]
    public async Task L_ecran_des_achats_propose_les_gestes_du_metier()
    {
        var ecran = Achats();

        ecran.Actions.Select(a => a.Libelle).Should().Equal(
            "Confirmer la commande", "Enregistrer la réception", "Annuler l'achat");

        // Annuler se distingue : c'est la seule action qui défait du travail.
        ecran.Actions.Single(a => a.Destructive).Libelle.Should().Be("Annuler l'achat");

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Une_action_sans_ligne_choisie_le_dit_plutot_que_d_echouer()
    {
        var ecran = Achats();

        await ecran.ConfirmerCommand.ExecuteAsync(null);

        _dialogue.Messages.Should().ContainSingle(m => m.Niveau == "avertissement");
        _dialogue.Messages[0].Message.Should().Contain("Choisissez d'abord une ligne");
    }

    [Fact]
    public async Task Receptionner_un_achat_confirme_fait_entrer_les_matieres_en_stock()
    {
        var achatId = await CreerAchatAsync(quantite: 25m);
        await _atelier.Achats.ConfirmerAsync(achatId);

        var ecran = Achats();
        await ecran.ChargerAsync();

        ecran.ElementSelectionne = ecran.Elements.Single(a => a.Id == achatId);

        await ecran.ReceptionnerCommand.ExecuteAsync(null);

        ecran.MessageErreur.Should().BeNull();

        var achat = await _atelier.Achats.ObtenirAsync(achatId);
        achat.Statut.Should().Be(PurchaseStatus.Recu);
        achat.Lignes[0].QuantiteRecue.Should().Be(25m);
    }

    [Fact]
    public async Task Receptionner_deux_fois_le_meme_achat_est_refuse_avec_un_message_clair()
    {
        var achatId = await CreerAchatAsync();
        await _atelier.Achats.ConfirmerAsync(achatId);

        var ecran = Achats();
        await ecran.ChargerAsync();
        ecran.ElementSelectionne = ecran.Elements.Single(a => a.Id == achatId);

        await ecran.ReceptionnerCommand.ExecuteAsync(null);
        await ecran.RafraichirCommand.ExecuteAsync(null);

        ecran.ElementSelectionne = ecran.Elements.Single(a => a.Id == achatId);
        await ecran.ReceptionnerCommand.ExecuteAsync(null);

        // Le message vient du métier et parle français : pas de trace technique.
        ecran.MessageErreur.Should().NotBeNullOrWhiteSpace();
        _dialogue.Messages.Should().Contain(m => m.Niveau == "erreur");
    }

    [Fact]
    public async Task Receptionner_un_brouillon_est_refuse()
    {
        var achatId = await CreerAchatAsync();

        var ecran = Achats();
        await ecran.ChargerAsync();
        ecran.ElementSelectionne = ecran.Elements.Single(a => a.Id == achatId);

        await ecran.ReceptionnerCommand.ExecuteAsync(null);

        ecran.MessageErreur.Should().Contain("Confirmez l'achat");
    }

    [Fact]
    public async Task Une_action_refusee_par_l_utilisateur_ne_change_rien()
    {
        var achatId = await CreerAchatAsync();
        _dialogue.ReponseConfirmation = false;

        var ecran = Achats();
        await ecran.ChargerAsync();
        ecran.ElementSelectionne = ecran.Elements.Single(a => a.Id == achatId);

        await ecran.ConfirmerCommand.ExecuteAsync(null);

        var achat = await _atelier.Achats.ObtenirAsync(achatId);
        achat.Statut.Should().Be(PurchaseStatus.Brouillon);
    }

    // ----------------------------------------------------------- Cuissons

    [Fact]
    public async Task Defourner_libere_le_four_et_enregistre_les_pieces()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase bleu");

        var four = await _atelier.Fours.CreerAsync(new FourRequete
        {
            Nom = "Four à gaz",
            Capacite = 100m,
            TemperatureMin = 800m,
            TemperatureMax = 1300m
        });

        var cuisson = await _atelier.Cuissons.CreerAsync(new CuissonRequete
        {
            FourId = four.Id,
            Type = FiringType.PremiereCuisson,
            Temperature = 980m,
            CoutEnergie = 1500m,
            Pieces = new List<PieceCuissonRequete>
            {
                new() { ProduitId = produitId, Quantite = 12m }
            }
        });

        await _atelier.Cuissons.DemarrerAsync(cuisson.Id);

        var ecran = new CuissonsVueModele(_atelier.Cuissons, new ServiceLangue(), Outils());
        await ecran.ChargerAsync();

        ecran.ElementSelectionne = ecran.Elements.Single(c => c.Id == cuisson.Id);

        await ecran.DefournerCommand.ExecuteAsync(null);

        ecran.MessageErreur.Should().BeNull();

        var defournee = await _atelier.Cuissons.ObtenirAsync(cuisson.Id);
        defournee.Statut.Should().Be(FiringBatchStatus.Terminee);
        defournee.Pieces[0].QuantiteAcceptee.Should().Be(12m);

        var fours = await _atelier.Fours.ListerAsync();
        fours.Single(f => f.Id == four.Id).Statut.Should().Be(KilnStatus.Disponible);
    }

    // ------------------------------------------------------------- Ventes

    [Fact]
    public async Task Annuler_une_vente_remet_les_produits_en_stock()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase bleu", prixVente: 4200m, stockInitial: 10);
        var clientId = await _atelier.CreerClientAsync();

        var vente = await _atelier.Ventes.EnregistrerAsync(
            new Application.DTOs.Commercial.VenteRequete
            {
                ClientId = clientId,
                Lignes = new List<Application.DTOs.Commercial.LigneVenteRequete>
                {
                    new() { ProduitId = produitId, Quantite = 3m, PrixUnitaire = 4200m }
                }
            });

        (await _atelier.StockProduitAsync(produitId)).Should().Be(7m);

        var ecran = new VentesVueModele(
            _atelier.Ventes, new DocumentMinimal(), new ServiceLangue(), Outils());

        await ecran.ChargerAsync();
        ecran.ElementSelectionne = ecran.Elements.Single(v => v.Id == vente.Id);

        await ecran.AnnulerCommand.ExecuteAsync(null);

        ecran.MessageErreur.Should().BeNull();
        (await _atelier.StockProduitAsync(produitId)).Should().Be(10m);
    }
}
