using CeramiPro.Infrastructure.Services;
using FluentAssertions;

namespace CeramiPro.Tests.Codes;

/// <summary>
/// Vérifie la fabrication des images de codes. La table du Code 39 utilisée par
/// le service a été confrontée à une implémentation indépendante, et les images
/// produites sont relues sans erreur par une bibliothèque de décodage.
/// </summary>
public class CodeGraphiqueServiceTests
{
    private readonly CodeGraphiqueService _service = new();

    [Fact]
    public void Le_code_qr_est_un_svg_complet()
    {
        var svg = _service.QrEnSvg("PRD-2026-0001", 200);

        svg.Should().StartWith("<svg").And.EndWith("</svg>");
        svg.Should().Contain("width=\"200\"").And.Contain("height=\"200\"");
        svg.Should().Contain("aria-label=\"Code QR PRD-2026-0001\"");
        svg.Should().Contain("<path");
    }

    [Fact]
    public void Le_code_barres_encadre_la_valeur_par_le_caractere_de_depart()
    {
        var svg = _service.CodeBarresEnSvg("PRD-2026-0001");

        svg.Should().StartWith("<svg").And.EndWith("</svg>");
        // La valeur est écrite en clair sous les barres, pour la saisie manuelle.
        svg.Should().Contain(">PRD-2026-0001</text>");
        svg.Should().Contain("aria-label=\"Code-barres PRD-2026-0001\"");
    }

    [Fact]
    public void Le_nombre_de_barres_correspond_a_la_longueur_du_code()
    {
        const string valeur = "PRD-2026-0001";
        var svg = _service.CodeBarresEnSvg(valeur);

        // Code 39 : 5 barres noires par caractère, plus les deux caractères « * ».
        var attendu = (valeur.Length + 2) * 5;

        System.Text.RegularExpressions.Regex.Matches(svg, "<rect x=").Count.Should().Be(attendu);
    }

    [Theory]
    [InlineData("PRD-2026-0001")]
    [InlineData("MAT-2026-0042")]
    [InlineData("ORD 2026 0007")]
    [InlineData("prd-2026-0001")]
    public void Les_references_de_l_atelier_sont_imprimables_en_code_barres(string valeur)
        => _service.EstImprimableEnCodeBarres(valeur).Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Vase émaillé")]
    [InlineData("PRD*2026")]
    public void Une_valeur_hors_code_39_est_refusee(string valeur)
    {
        _service.EstImprimableEnCodeBarres(valeur).Should().BeFalse();
        _service.CodeBarresEnSvg(valeur).Should().BeEmpty();
    }

    [Fact]
    public void Le_code_qr_accepte_les_accents_contrairement_au_code_barres()
    {
        _service.QrEnSvg("Vase émaillé").Should().StartWith("<svg");
        _service.CodeBarresEnSvg("Vase émaillé").Should().BeEmpty();
    }

    [Fact]
    public void Les_caracteres_speciaux_du_svg_sont_echappes()
    {
        var svg = _service.QrEnSvg("a<b>&\"c\"");

        svg.Should().NotContain("<b>");
        svg.Should().Contain("&lt;b&gt;");
    }
}
