using CeramicWorkshop.Application.DTOs.Auth;
using FluentValidation;

namespace CeramicWorkshop.Application.Validators;

public class ConnexionRequeteValidator : AbstractValidator<ConnexionRequete>
{
    public ConnexionRequeteValidator()
    {
        RuleFor(x => x.NomUtilisateur)
            .NotEmpty().WithMessage("Le nom d'utilisateur est obligatoire.")
            .MaximumLength(60).WithMessage("Le nom d'utilisateur ne doit pas dépasser 60 caractères.");

        RuleFor(x => x.MotDePasse)
            .NotEmpty().WithMessage("Le mot de passe est obligatoire.");
    }
}

public class ChangementMotDePasseRequeteValidator : AbstractValidator<ChangementMotDePasseRequete>
{
    public ChangementMotDePasseRequeteValidator()
    {
        RuleFor(x => x.MotDePasseActuel)
            .NotEmpty().WithMessage("Le mot de passe actuel est obligatoire.");

        RuleFor(x => x.NouveauMotDePasse)
            .SetValidator(new MotDePasseValidator());

        RuleFor(x => x.ConfirmationMotDePasse)
            .Equal(x => x.NouveauMotDePasse)
            .WithMessage("Le nouveau mot de passe et sa confirmation ne correspondent pas.");
    }
}

public class RenouvellementRequeteValidator : AbstractValidator<RenouvellementRequete>
{
    public RenouvellementRequeteValidator()
    {
        RuleFor(x => x.JetonRenouvellement)
            .NotEmpty().WithMessage("La session est expirée. Veuillez vous reconnecter.");
    }
}

/// <summary>Exigences minimales de robustesse d'un mot de passe.</summary>
public class MotDePasseValidator : AbstractValidator<string>
{
    public const int LongueurMinimale = 8;

    public MotDePasseValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Le mot de passe est obligatoire.")
            .MinimumLength(LongueurMinimale)
            .WithMessage($"Le mot de passe doit contenir au moins {LongueurMinimale} caractères.")
            .MaximumLength(128).WithMessage("Le mot de passe ne doit pas dépasser 128 caractères.")
            .Matches("[A-Za-zÀ-ÿ]").WithMessage("Le mot de passe doit contenir au moins une lettre.")
            .Matches("[0-9]").WithMessage("Le mot de passe doit contenir au moins un chiffre.");
    }
}
