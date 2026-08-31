namespace CeramiPro.Application.DTOs.Settings;

/// <summary>Paramètres de l'atelier affichés et modifiables dans l'écran « Paramètres ».</summary>
public class ParametresAtelierDto
{
    public string NomAtelier { get; set; } = string.Empty;
    public string? RaisonSociale { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? SiteWeb { get; set; }
    public string? CheminLogo { get; set; }

    public string? NumeroIdentificationFiscale { get; set; }
    public string? RegistreCommerce { get; set; }
    public string? ArticleImposition { get; set; }

    public string CodeDevise { get; set; } = "DZD";
    public string SymboleDevise { get; set; } = "DA";
    public string Culture { get; set; } = "fr-DZ";
    public string FuseauHoraire { get; set; } = "Africa/Algiers";
    public string FormatDate { get; set; } = "dd/MM/yyyy";
    public int DecimalesDevise { get; set; } = 2;

    public decimal TauxTvaParDefaut { get; set; } = 19m;
    public bool TvaActivee { get; set; }

    public string PrefixeFacture { get; set; } = "FAC";
    public string PrefixeVente { get; set; } = "VTE";
    public string PrefixeAchat { get; set; } = "ACH";
    public string PrefixeProduction { get; set; } = "PROD";
    public string PrefixeCommande { get; set; } = "CMD";
    public string PrefixePaiement { get; set; } = "PAI";
    public string PrefixeCuisson { get; set; } = "CUIS";

    public string? PiedDePageFacture { get; set; }
}
