using CeramicWorkshop.Application.Common;
using FluentAssertions;

namespace CeramicWorkshop.UnitTests.General;

public class SimilitudeTests
{
    [Theory]
    [InlineData("Émaillé", "emaille")]
    [InlineData("  VASE  ", "vase")]
    [InlineData("Décoration", "decoration")]
    public void Aplatir_retire_les_accents_et_les_espaces(string texte, string attendu)
        => Similitude.Aplatir(texte).Should().Be(attendu);

    [Fact]
    public void Une_correspondance_exacte_vaut_cent()
        => Similitude.Noter("vase", "Vase").Should().Be(100);

    [Fact]
    public void Un_texte_sans_accent_retrouve_le_texte_accentue()
        => Similitude.Noter("emaille", "Émaillé").Should().Be(100);

    [Fact]
    public void Un_debut_de_nom_est_bien_note()
        => Similitude.Noter("vas", "Vase décoratif").Should().BeGreaterThanOrEqualTo(90);

    [Fact]
    public void Un_mot_situe_au_milieu_du_nom_est_retrouve()
        => Similitude.Noter("decoratif", "Vase décoratif bleu")
            .Should().BeGreaterThanOrEqualTo(Similitude.SeuilPertinence);

    [Fact]
    public void Une_faute_de_frappe_reste_au_dessus_du_seuil()
        => Similitude.Noter("assiete", "Assiette").Should().BeGreaterThanOrEqualTo(Similitude.SeuilPertinence);

    [Fact]
    public void Deux_mots_sans_rapport_restent_sous_le_seuil()
        => Similitude.Noter("vase", "Fournisseur Tlemcen").Should().BeLessThan(Similitude.SeuilPertinence);

    [Theory]
    [InlineData("", "vase")]
    [InlineData("vase", "")]
    [InlineData(null, "vase")]
    public void Un_texte_vide_ne_correspond_a_rien(string? recherche, string candidat)
        => Similitude.Noter(recherche, candidat).Should().Be(0);

    [Theory]
    [InlineData("chat", "chat", 0)]
    [InlineData("chat", "chats", 1)]
    [InlineData("chat", "chien", 3)]
    [InlineData("", "abc", 3)]
    public void La_distance_compte_les_corrections(string gauche, string droite, int attendu)
        => Similitude.Distance(gauche, droite).Should().Be(attendu);
}
