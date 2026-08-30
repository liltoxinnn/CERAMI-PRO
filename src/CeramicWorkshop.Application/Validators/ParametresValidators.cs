using System.Linq.Expressions;
using CeramicWorkshop.Application.DTOs.Settings;
using FluentValidation;

namespace CeramicWorkshop.Application.Validators;

public class ParametresAtelierDtoValidator : AbstractValidator<ParametresAtelierDto>
{
    public ParametresAtelierDtoValidator()
    {
        RuleFor(x => x.NomAtelier)
            .NotEmpty().WithMessage("Le nom de l'atelier est obligatoire.")
            .MaximumLength(150).WithMessage("Le nom de l'atelier ne doit pas dépasser 150 caractères.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("L'adresse email n'est pas valide.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.CodeDevise)
            .NotEmpty().WithMessage("Le code de la devise est obligatoire.")
            .Length(3).WithMessage("Le code de la devise doit contenir 3 lettres (exemple : DZD).");

        RuleFor(x => x.SymboleDevise)
            .NotEmpty().WithMessage("Le symbole de la devise est obligatoire.")
            .MaximumLength(10).WithMessage("Le symbole de la devise ne doit pas dépasser 10 caractères.");

        RuleFor(x => x.Culture)
            .NotEmpty().WithMessage("La langue est obligatoire.")
            .MaximumLength(10).WithMessage("Le code de langue ne doit pas dépasser 10 caractères.");

        RuleFor(x => x.FuseauHoraire)
            .NotEmpty().WithMessage("Le fuseau horaire est obligatoire.")
            .MaximumLength(60).WithMessage("Le fuseau horaire ne doit pas dépasser 60 caractères.");

        RuleFor(x => x.FormatDate)
            .NotEmpty().WithMessage("Le format de date est obligatoire.")
            .MaximumLength(20).WithMessage("Le format de date ne doit pas dépasser 20 caractères.");

        RuleFor(x => x.DecimalesDevise)
            .InclusiveBetween(0, 4).WithMessage("Le nombre de décimales doit être compris entre 0 et 4.");

        RuleFor(x => x.TauxTvaParDefaut)
            .InclusiveBetween(0m, 100m).WithMessage("Le taux de TVA doit être compris entre 0 et 100 %.");

        ReglePrefixe(x => x.PrefixeFacture, "facture");
        ReglePrefixe(x => x.PrefixeVente, "vente");
        ReglePrefixe(x => x.PrefixeAchat, "achat");
        ReglePrefixe(x => x.PrefixeProduction, "production");
        ReglePrefixe(x => x.PrefixeCommande, "commande personnalisée");
        ReglePrefixe(x => x.PrefixePaiement, "paiement");
        ReglePrefixe(x => x.PrefixeCuisson, "cuisson");
    }

    private void ReglePrefixe(Expression<Func<ParametresAtelierDto, string>> propriete, string libelle)
        => RuleFor(propriete)
            .NotEmpty().WithMessage($"Le préfixe de numérotation des documents de {libelle} est obligatoire.")
            .MaximumLength(10).WithMessage($"Le préfixe de {libelle} ne doit pas dépasser 10 caractères.");
}
