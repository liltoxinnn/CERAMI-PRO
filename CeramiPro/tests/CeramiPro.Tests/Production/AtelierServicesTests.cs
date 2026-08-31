using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Domain.Enums;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Tests.Production;

public class CuissonServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    private async Task<FourDto> CreerFourAsync(decimal capacite = 50m)
        => await _atelier.Fours.CreerAsync(new FourRequete
        {
            Nom = "Four 1", Capacite = capacite, TemperatureMin = 800m, TemperatureMax = 1250m
        });

    [Fact]
    public async Task Un_four_recoit_une_reference_lisible()
    {
        var four = await CreerFourAsync();

        four.Reference.Should().Be("FOUR-01");
        four.Statut.Should().Be(KilnStatus.Disponible);
    }

    [Fact]
    public async Task Une_temperature_maximale_inferieure_au_minimum_est_refusee()
    {
        var action = async () => await _atelier.Fours.CreerAsync(new FourRequete
        {
            Nom = "Four incohérent", Capacite = 10m, TemperatureMin = 1200m, TemperatureMax = 900m
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*supérieure*");
    }

    [Fact]
    public async Task Une_cuisson_ne_peut_pas_depasser_la_capacite_du_four()
    {
        var four = await CreerFourAsync(capacite: 30m);
        var produitId = await _atelier.CreerProduitAsync("Vase");

        var action = async () => await _atelier.Cuissons.CreerAsync(new CuissonRequete
        {
            FourId = four.Id,
            Temperature = 1050m,
            Pieces = new List<PieceCuissonRequete> { new() { ProduitId = produitId, Quantite = 45m } }
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*capacité*");
    }

    [Fact]
    public async Task Une_temperature_hors_plage_du_four_est_refusee()
    {
        var four = await CreerFourAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase");

        var action = async () => await _atelier.Cuissons.CreerAsync(new CuissonRequete
        {
            FourId = four.Id,
            Temperature = 1400m,
            Pieces = new List<PieceCuissonRequete> { new() { ProduitId = produitId, Quantite = 5m } }
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*température*");
    }

    [Fact]
    public async Task Le_demarrage_occupe_le_four()
    {
        var four = await CreerFourAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase");

        var cuisson = await _atelier.Cuissons.CreerAsync(new CuissonRequete
        {
            FourId = four.Id, Temperature = 1050m,
            Pieces = new List<PieceCuissonRequete> { new() { ProduitId = produitId, Quantite = 10m } }
        });

        await _atelier.Cuissons.DemarrerAsync(cuisson.Id);

        var fourApres = (await _atelier.Fours.ListerAsync()).Single();
        fourApres.Statut.Should().Be(KilnStatus.EnCuisson);
    }

    [Fact]
    public async Task Le_defournement_repartit_le_cout_energetique_et_libere_le_four()
    {
        var four = await CreerFourAsync();
        var vase = await _atelier.CreerProduitAsync("Vase");
        var assiette = await _atelier.CreerProduitAsync("Assiette");

        var cuisson = await _atelier.Cuissons.CreerAsync(new CuissonRequete
        {
            FourId = four.Id, Temperature = 1050m, CoutEnergie = 2500m,
            Pieces = new List<PieceCuissonRequete>
            {
                new() { ProduitId = vase, Quantite = 10m },
                new() { ProduitId = assiette, Quantite = 40m }
            }
        });

        await _atelier.Cuissons.DemarrerAsync(cuisson.Id);
        _atelier.Horloge.Avancer(TimeSpan.FromHours(6.5));

        var defourne = await _atelier.Cuissons.DefournerAsync(cuisson.Id, new DefournementRequete
        {
            CoutEnergie = 2500m,
            Pieces = cuisson.Pieces.Select(p => new ResultatPieceRequete
            {
                PieceId = p.Id, QuantiteAcceptee = p.Quantite
            }).ToList()
        });

        defourne.Statut.Should().Be(FiringBatchStatus.Terminee);
        defourne.DureeHeures.Should().Be(6.5m);

        // 2 500 DA répartis au prorata : 500 DA pour 10 pièces, 2 000 DA pour 40.
        defourne.Pieces.Single(p => p.ProduitNom == "Vase").CoutEnergieImpute.Should().Be(500m);
        defourne.Pieces.Single(p => p.ProduitNom == "Assiette").CoutEnergieImpute.Should().Be(2000m);

        (await _atelier.Fours.ListerAsync()).Single().Statut.Should().Be(KilnStatus.Disponible);
    }

    [Fact]
    public async Task Le_cout_de_cuisson_remonte_dans_la_production()
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await _atelier.Production.CreerAsync(new OrdreProductionRequete
        {
            ProduitId = produitId, QuantitePrevue = 10m
        });
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());

        var four = await CreerFourAsync();
        var cuisson = await _atelier.Cuissons.CreerAsync(new CuissonRequete
        {
            FourId = four.Id, Temperature = 1050m, CoutEnergie = 1800m,
            Pieces = new List<PieceCuissonRequete>
            {
                new() { ProductionId = ordre.Id, ProduitId = produitId, Quantite = 10m }
            }
        });

        await _atelier.Cuissons.DemarrerAsync(cuisson.Id);
        await _atelier.Cuissons.DefournerAsync(cuisson.Id, new DefournementRequete
        {
            CoutEnergie = 1800m,
            Pieces = cuisson.Pieces.Select(p => new ResultatPieceRequete
            {
                PieceId = p.Id, QuantiteAcceptee = 9m, QuantiteEndommagee = 1m
            }).ToList()
        });

        var apres = await _atelier.Production.ObtenirAsync(ordre.Id);
        apres.CoutCuisson.Should().Be(1800m);
        apres.QuantiteEndommagee.Should().Be(1m);
    }

    [Fact]
    public async Task Un_resultat_de_defournement_superieur_a_l_enfourne_est_refuse()
    {
        var four = await CreerFourAsync();
        var produitId = await _atelier.CreerProduitAsync("Vase");

        var cuisson = await _atelier.Cuissons.CreerAsync(new CuissonRequete
        {
            FourId = four.Id, Temperature = 1050m,
            Pieces = new List<PieceCuissonRequete> { new() { ProduitId = produitId, Quantite = 10m } }
        });
        await _atelier.Cuissons.DemarrerAsync(cuisson.Id);

        var action = async () => await _atelier.Cuissons.DefournerAsync(cuisson.Id, new DefournementRequete
        {
            Pieces = new List<ResultatPieceRequete>
            {
                new() { PieceId = cuisson.Pieces[0].Id, QuantiteAcceptee = 9m, QuantiteEndommagee = 5m }
            }
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*dépasse la quantité enfournée*");
    }

    public void Dispose() => _atelier.Dispose();
}

public class QualiteServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    private async Task<int> ProductionLanceeAsync(decimal quantite = 20m)
    {
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await _atelier.Production.CreerAsync(new OrdreProductionRequete
        {
            ProduitId = produitId, QuantitePrevue = quantite
        });
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());
        return ordre.Id;
    }

    [Fact]
    public async Task Un_controle_sans_defaut_est_conforme()
    {
        var productionId = await ProductionLanceeAsync();

        var controle = await _atelier.Qualite.EnregistrerAsync(new ControleQualiteRequete
        {
            ProductionId = productionId, QuantiteControlee = 20m, QuantiteAcceptee = 20m
        });

        controle.Resultat.Should().Be(QualityResult.Conforme);
        controle.Reference.Should().StartWith("QUA-");
    }

    [Fact]
    public async Task Un_point_de_controle_non_conforme_demande_une_retouche()
    {
        var productionId = await ProductionLanceeAsync();

        var controle = await _atelier.Qualite.EnregistrerAsync(new ControleQualiteRequete
        {
            ProductionId = productionId,
            QuantiteControlee = 20m,
            QuantiteAcceptee = 17m,
            QuantiteARetoucher = 3m,
            EmailConforme = false
        });

        controle.Resultat.Should().Be(QualityResult.RetoucheNecessaire);
        controle.EmailConforme.Should().BeFalse();
    }

    [Fact]
    public async Task Des_pieces_refusees_rendent_le_controle_non_conforme()
    {
        var productionId = await ProductionLanceeAsync();

        var controle = await _atelier.Qualite.EnregistrerAsync(new ControleQualiteRequete
        {
            ProductionId = productionId, QuantiteControlee = 20m, QuantiteAcceptee = 15m, QuantiteRefusee = 5m
        });

        controle.Resultat.Should().Be(QualityResult.NonConforme);
    }

    [Fact]
    public async Task Les_pieces_refusees_sont_comptees_comme_endommagees()
    {
        var productionId = await ProductionLanceeAsync();

        await _atelier.Qualite.EnregistrerAsync(new ControleQualiteRequete
        {
            ProductionId = productionId, QuantiteControlee = 20m, QuantiteAcceptee = 16m, QuantiteRefusee = 4m
        });

        (await _atelier.Production.ObtenirAsync(productionId)).QuantiteEndommagee.Should().Be(4m);
    }

    [Fact]
    public async Task Les_defauts_releves_sont_conserves_avec_leur_gravite()
    {
        var productionId = await ProductionLanceeAsync();

        var controle = await _atelier.Qualite.EnregistrerAsync(new ControleQualiteRequete
        {
            ProductionId = productionId,
            QuantiteControlee = 20m,
            QuantiteAcceptee = 18m,
            QuantiteARetoucher = 2m,
            FissuresConformes = false,
            Defauts = new List<DefautQualiteRequete>
            {
                new()
                {
                    PointControle = QualityCheckPoint.Fissures,
                    Gravite = IssueSeverity.Majeure,
                    Solution = IssueResolution.Retouche,
                    Quantite = 2m,
                    Description = "Micro-fissures sur le col",
                    Remede = "Reprise à l'émail"
                }
            }
        });

        var defaut = controle.Defauts.Single();
        defaut.PointControleLibelle.Should().Be("Fissures");
        defaut.GraviteLibelle.Should().Be("Majeure");
        defaut.SolutionLibelle.Should().Be("Retouche");
    }

    [Fact]
    public async Task Un_total_superieur_aux_pieces_controlees_est_refuse()
    {
        var productionId = await ProductionLanceeAsync();

        var action = async () => await _atelier.Qualite.EnregistrerAsync(new ControleQualiteRequete
        {
            ProductionId = productionId, QuantiteControlee = 20m, QuantiteAcceptee = 18m, QuantiteRefusee = 5m
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*dépasse*");
    }

    public void Dispose() => _atelier.Dispose();
}

public class DecorationServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    private async Task<int> TypeDecorationAsync()
    {
        var type = new Domain.Entities.Decoration.DecorationType { Name = "Dorure" };
        _atelier.Contexte.DecorationTypes.Add(type);
        await _atelier.Contexte.SaveChangesAsync();
        return type.Id;
    }

    [Fact]
    public async Task Un_travail_de_decoration_est_cree_planifie()
    {
        var typeId = await TypeDecorationAsync();

        var decoration = await _atelier.Decorations.CreerAsync(new DecorationRequete
        {
            TypeDecorationId = typeId, Quantite = 20m, Couleurs = "Blanc et or", QuantiteOr = 100m, Cout = 4000m
        });

        decoration.Reference.Should().StartWith("DEC-");
        decoration.Statut.Should().Be(DecorationStatus.Planifiee);
        decoration.QuantiteOr.Should().Be(100m);
    }

    [Fact]
    public async Task Le_cout_de_decoration_remonte_dans_la_production_a_la_cloture()
    {
        var typeId = await TypeDecorationAsync();
        var (produitId, _, _) = await _atelier.PreparerVaseAsync();
        var ordre = await _atelier.Production.CreerAsync(new OrdreProductionRequete
        {
            ProduitId = produitId, QuantitePrevue = 20m
        });
        await _atelier.Production.LancerAsync(ordre.Id, new LancementProductionRequete());

        var decoration = await _atelier.Decorations.CreerAsync(new DecorationRequete
        {
            TypeDecorationId = typeId, ProductionId = ordre.Id, Quantite = 20m, Cout = 4000m
        });

        await _atelier.Decorations.ChangerStatutAsync(decoration.Id, DecorationStatus.EnCours);
        await _atelier.Decorations.ChangerStatutAsync(decoration.Id, DecorationStatus.Terminee);

        (await _atelier.Production.ObtenirAsync(ordre.Id)).CoutDecoration.Should().Be(4000m);
    }

    [Fact]
    public async Task Une_decoration_terminee_ne_peut_plus_etre_modifiee()
    {
        var typeId = await TypeDecorationAsync();
        var decoration = await _atelier.Decorations.CreerAsync(new DecorationRequete
        {
            TypeDecorationId = typeId, Quantite = 5m
        });
        await _atelier.Decorations.ChangerStatutAsync(decoration.Id, DecorationStatus.Terminee);

        var action = async () => await _atelier.Decorations.ModifierAsync(decoration.Id, new DecorationRequete
        {
            TypeDecorationId = typeId, Quantite = 10m
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*terminé*");
    }

    [Fact]
    public async Task Une_photo_du_decor_peut_etre_ajoutee()
    {
        var typeId = await TypeDecorationAsync();
        var decoration = await _atelier.Decorations.CreerAsync(new DecorationRequete
        {
            TypeDecorationId = typeId, Quantite = 5m
        });

        var apres = await _atelier.Decorations.AjouterPhotoAsync(
            decoration.Id, "/fichiers/2026-03/decor.jpg", "Décor terminé");

        apres.Photos.Should().ContainSingle().Which.Should().Be("/fichiers/2026-03/decor.jpg");
    }

    public void Dispose() => _atelier.Dispose();
}
