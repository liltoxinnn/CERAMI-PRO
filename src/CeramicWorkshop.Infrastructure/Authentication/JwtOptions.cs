namespace CeramicWorkshop.Infrastructure.Authentication;

/// <summary>Paramètres des jetons d'authentification, lus depuis la configuration.</summary>
public class JwtOptions
{
    public const string Section = "Jwt";

    /// <summary>Clé secrète de signature. Elle ne doit jamais être publiée ni versionnée.</summary>
    public string Cle { get; set; } = string.Empty;

    public string Emetteur { get; set; } = "CeramiPro";
    public string Audience { get; set; } = "CeramiPro";

    /// <summary>Durée de validité du jeton d'accès, en minutes.</summary>
    public int DureeMinutes { get; set; } = 120;

    /// <summary>Longueur minimale acceptée pour la clé de signature (256 bits).</summary>
    public const int LongueurCleMinimale = 32;
}
