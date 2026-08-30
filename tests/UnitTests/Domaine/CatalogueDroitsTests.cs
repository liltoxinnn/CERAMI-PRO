using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Enums;
using FluentAssertions;

namespace CeramicWorkshop.UnitTests.Domaine;

public class CatalogueDroitsTests
{
    [Fact]
    public void Aucun_droit_n_est_declare_en_double()
    {
        PermissionCodes.Catalogue.Select(d => d.Code).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Chaque_droit_possede_un_libelle_et_un_module_en_francais()
    {
        PermissionCodes.Catalogue.Should().OnlyContain(d =>
            !string.IsNullOrWhiteSpace(d.Nom) && !string.IsNullOrWhiteSpace(d.Module));
    }

    [Fact]
    public void Les_droits_par_defaut_existent_tous_dans_le_catalogue()
    {
        var codesConnus = PermissionCodes.Catalogue.Select(d => d.Code).ToHashSet();

        foreach (var (role, droits) in PermissionCodes.DroitsParDefaut)
        {
            droits.Should().OnlyContain(code => codesConnus.Contains(code),
                $"les droits par défaut du rôle « {role} » doivent exister dans le catalogue");
        }
    }

    [Fact]
    public void Chaque_role_du_logiciel_a_des_droits_par_defaut()
    {
        foreach (var (code, _, _) in RoleCodes.Catalogue)
        {
            PermissionCodes.DroitsParDefaut.Should().ContainKey(code);
        }
    }

    [Fact]
    public void Seul_l_administrateur_peut_gerer_les_utilisateurs()
    {
        foreach (var (role, droits) in PermissionCodes.DroitsParDefaut.Where(r => r.Key != RoleCodes.Administrateur))
        {
            droits.Should().NotContain(PermissionCodes.UtilisateursGerer,
                $"le rôle « {role} » ne doit pas gérer les comptes");
        }
    }
}

public class LibellesFrancaisTests
{
    [Fact]
    public void Les_etapes_de_production_sont_affichees_en_francais()
    {
        ProductionStatus.Faconnage.Libelle().Should().Be("Façonnage");
        ProductionStatus.PremiereCuisson.Libelle().Should().Be("Première cuisson");
        ProductionStatus.ControleQualite.Libelle().Should().Be("Contrôle qualité");
    }

    [Fact]
    public void Les_etapes_d_une_commande_personnalisee_sont_affichees_en_francais()
    {
        CustomOrderStatus.ValidationClient.Libelle().Should().Be("Validation client");
        CustomOrderStatus.Livre.Libelle().Should().Be("Livré");
    }

    [Fact]
    public void Tous_les_statuts_de_production_ont_un_libelle_accentue_ou_explicite()
    {
        var libelles = EnumExtensions.Libelles<ProductionStatus>();

        libelles.Should().HaveCount(10);
        libelles.Should().OnlyContain(l => !string.IsNullOrWhiteSpace(l.Libelle));
    }

    [Fact]
    public void Les_types_de_mouvement_de_stock_sont_traduits()
    {
        InventoryTransactionType.ConsommationProduction.Libelle().Should().Be("Consommation en production");
        InventoryTransactionType.Endommage.Libelle().Should().Be("Produit endommagé");
    }
}
