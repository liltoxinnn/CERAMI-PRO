namespace CeramicWorkshop.Application.Interfaces;

/// <summary>Hachage et vérification des mots de passe. Aucun mot de passe n'est stocké en clair.</summary>
public interface IPasswordHasherService
{
    string Hacher(string motDePasse);

    bool Verifier(string motDePasse, string empreinte);
}
