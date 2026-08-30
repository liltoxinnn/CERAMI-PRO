using System.Security.Cryptography;
using CeramicWorkshop.Application.Interfaces;

namespace CeramicWorkshop.Infrastructure.Authentication;

/// <summary>
/// Hachage des mots de passe avec PBKDF2 (HMAC-SHA512), un sel aléatoire par
/// utilisateur et une comparaison à temps constant.
/// Format stocké : « v1.itérations.sel.empreinte » (les deux derniers en Base64).
/// </summary>
public class PasswordHasherService : IPasswordHasherService
{
    private const string Version = "v1";
    private const int Iterations = 210_000;
    private const int TailleSelOctets = 16;
    private const int TailleEmpreinteOctets = 32;

    private static readonly HashAlgorithmName Algorithme = HashAlgorithmName.SHA512;

    public string Hacher(string motDePasse)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motDePasse);

        var sel = RandomNumberGenerator.GetBytes(TailleSelOctets);
        var empreinte = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, Iterations, Algorithme, TailleEmpreinteOctets);

        return string.Join('.', Version, Iterations, Convert.ToBase64String(sel), Convert.ToBase64String(empreinte));
    }

    public bool Verifier(string motDePasse, string empreinteStockee)
    {
        if (string.IsNullOrWhiteSpace(motDePasse) || string.IsNullOrWhiteSpace(empreinteStockee))
        {
            return false;
        }

        var parties = empreinteStockee.Split('.');
        if (parties.Length != 4 || parties[0] != Version || !int.TryParse(parties[1], out var iterations))
        {
            return false;
        }

        try
        {
            var sel = Convert.FromBase64String(parties[2]);
            var attendue = Convert.FromBase64String(parties[3]);
            var calculee = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, iterations, Algorithme, attendue.Length);

            return CryptographicOperations.FixedTimeEquals(calculee, attendue);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
