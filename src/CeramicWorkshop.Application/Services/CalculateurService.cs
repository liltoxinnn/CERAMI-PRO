using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Finances;
using CeramicWorkshop.Application.Interfaces;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Calculateurs de l'atelier. Les formules restent dans la couche métier :
/// l'écran se contente d'afficher le résultat renvoyé par le serveur.
/// </summary>
public class CalculateurService : ICalculateurService
{
    public CalculSurfaceDto Surface(CalculSurfaceRequete requete)
    {
        if (requete.Longueur <= 0 || requete.Largeur <= 0)
        {
            throw new BusinessRuleException("Indiquez une longueur et une largeur supérieures à zéro.");
        }

        if (requete.NombrePieces < 1)
        {
            throw new BusinessRuleException("Le nombre de pièces doit être d'au moins 1.");
        }

        if (requete.PourcentagePerte is < 0 or > 100)
        {
            throw new BusinessRuleException("Le pourcentage de perte doit être compris entre 0 et 100.");
        }

        var unitaire = Math.Round(requete.Longueur * requete.Largeur, 3, MidpointRounding.AwayFromZero);
        var totale = Math.Round(unitaire * requete.NombrePieces, 3, MidpointRounding.AwayFromZero);
        var perte = Math.Round(totale * requete.PourcentagePerte / 100m, 3, MidpointRounding.AwayFromZero);

        return new CalculSurfaceDto(unitaire, totale, perte, Math.Round(totale + perte, 3, MidpointRounding.AwayFromZero));
    }

    public CalculQuantiteDto Quantite(CalculQuantiteRequete requete)
    {
        if (requete.QuantiteParUnite <= 0)
        {
            throw new BusinessRuleException("La quantité contenue dans une unité doit être supérieure à zéro.");
        }

        if (requete.QuantiteSouhaitee <= 0)
        {
            throw new BusinessRuleException("Indiquez la quantité souhaitée.");
        }

        if (requete.PourcentagePerte is < 0 or > 100)
        {
            throw new BusinessRuleException("Le pourcentage de perte doit être compris entre 0 et 100.");
        }

        var avecPerte = Math.Round(
            requete.QuantiteSouhaitee * (1 + requete.PourcentagePerte / 100m), 3, MidpointRounding.AwayFromZero);

        var unites = (int)Math.Ceiling(avecPerte / requete.QuantiteParUnite);

        return new CalculQuantiteDto(requete.QuantiteSouhaitee, avecPerte, unites);
    }
}
