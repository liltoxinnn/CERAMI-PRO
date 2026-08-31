using CeramiPro.Application.Common;
using CeramiPro.Application.Localisation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Base des formulaires de saisie : décrit les champs, contrôle les valeurs
/// obligatoires et confie l'enregistrement au service métier.
///
/// Les règles de gestion restent dans les services : ce qui est vérifié ici
/// ne concerne que la saisie elle-même — un champ laissé vide, un nombre
/// négatif — afin d'éviter un aller-retour inutile jusqu'à la base.
/// </summary>
public abstract partial class FormulaireVueModele<TRequete> : ObservableObject, IFormulaire
    where TRequete : class, new()
{
    protected readonly IServiceLangue Langue;

    protected FormulaireVueModele(IServiceLangue langue) => Langue = langue;

    /// <summary>Objet renseigné par les contrôles du formulaire.</summary>
    public TRequete Requete { get; protected set; } = new();

    /// <summary>Identifiant modifié ; nul lors d'une création.</summary>
    public int? Id { get; protected set; }

    public bool EstCreation => Id is null;

    [ObservableProperty]
    private string? _messageErreur;

    [ObservableProperty]
    private bool _enregistrementEnCours;

    /// <summary>Vrai quand l'enregistrement a réussi : la fenêtre peut se fermer.</summary>
    [ObservableProperty]
    private bool _enregistre;

    public abstract string Titre { get; }

    /// <summary>Champs affichés, dans l'ordre de saisie.</summary>
    public abstract IReadOnlyList<ChampFormulaire> Champs { get; }

    public string LibelleEnregistrer => Langue["action.enregistrer"];
    public string LibelleAnnuler => Langue["action.annuler"];

    /// <summary>Enregistre auprès du service métier.</summary>
    protected abstract Task EnregistrerAsync();

    /// <summary>
    /// Charge les listes déroulantes du formulaire. Les formulaires qui
    /// n'en ont pas gardent le comportement par défaut : ne rien faire.
    /// </summary>
    public virtual Task PreparerAsync() => Task.CompletedTask;

    /// <summary>Charge les valeurs d'une fiche existante.</summary>
    public virtual Task PreparerModificationAsync(int id) => Task.CompletedTask;

    [RelayCommand]
    private async Task ValiderAsync()
    {
        MessageErreur = ChampsManquants();

        if (MessageErreur is not null)
        {
            return;
        }

        EnregistrementEnCours = true;

        try
        {
            await EnregistrerAsync();
            Enregistre = true;
        }
        catch (RegleMetierException erreur)
        {
            MessageErreur = erreur.Details.Count == 0
                ? erreur.Message
                : erreur.Message + "\n• " + string.Join("\n• ", erreur.Details);
        }
        catch (IntrouvableException erreur)
        {
            MessageErreur = erreur.Message;
        }
        catch (Exception)
        {
            MessageErreur = Langue["message.erreurEnregistrement"];
        }
        finally
        {
            EnregistrementEnCours = false;
        }
    }

    /// <summary>
    /// Nomme le premier champ obligatoire resté vide. Signaler le champ plutôt
    /// qu'un « champ obligatoire » anonyme évite à l'utilisateur de chercher.
    /// </summary>
    private string? ChampsManquants()
    {
        foreach (var champ in Champs.Where(c => c.Obligatoire))
        {
            var valeur = typeof(TRequete).GetProperty(champ.Propriete)?.GetValue(Requete);

            var vide = valeur switch
            {
                null => true,
                string texte => string.IsNullOrWhiteSpace(texte),
                int nombre => nombre == 0,
                decimal montant => montant == 0m,
                _ => false
            };

            if (vide)
            {
                return $"« {champ.Libelle} » : {Langue["etat.obligatoire"].ToLowerInvariant()}.";
            }
        }

        return null;
    }
}
