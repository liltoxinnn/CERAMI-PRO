using CeramicWorkshop.Application.DTOs.Identity;
using FluentValidation;

namespace CeramicWorkshop.Application.Validators;

public class CreerUtilisateurRequeteValidator : AbstractValidator<CreerUtilisateurRequete>
{
    public CreerUtilisateurRequeteValidator()
    {
        RuleFor(x => x.NomUtilisateur)
            .NotEmpty().WithMessage("Le nom d'utilisateur est obligatoire.")
            .MinimumLength(3).WithMessage("Le nom d'utilisateur doit contenir au moins 3 caractères.")
            .MaximumLength(60).WithMessage("Le nom d'utilisateur ne doit pas dépasser 60 caractères.")
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Le nom d'utilisateur ne peut contenir que des lettres, des chiffres, un point, un tiret ou un tiret bas.");

        RuleFor(x => x.NomComplet)
            .NotEmpty().WithMessage("Le nom complet est obligatoire.")
            .MaximumLength(150).WithMessage("Le nom complet ne doit pas dépasser 150 caractères.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("L'adresse email n'est pas valide.")
            .MaximumLength(150).WithMessage("L'adresse email ne doit pas dépasser 150 caractères.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Telephone)
            .MaximumLength(30).WithMessage("Le numéro de téléphone ne doit pas dépasser 30 caractères.")
            .When(x => !string.IsNullOrWhiteSpace(x.Telephone));

        RuleFor(x => x.MotDePasse).SetValidator(new MotDePasseValidator());

        RuleFor(x => x.RoleId)
            .GreaterThan(0).WithMessage("Le rôle est obligatoire.");
    }
}

public class ModifierUtilisateurRequeteValidator : AbstractValidator<ModifierUtilisateurRequete>
{
    public ModifierUtilisateurRequeteValidator()
    {
        RuleFor(x => x.NomComplet)
            .NotEmpty().WithMessage("Le nom complet est obligatoire.")
            .MaximumLength(150).WithMessage("Le nom complet ne doit pas dépasser 150 caractères.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("L'adresse email n'est pas valide.")
            .MaximumLength(150).WithMessage("L'adresse email ne doit pas dépasser 150 caractères.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Telephone)
            .MaximumLength(30).WithMessage("Le numéro de téléphone ne doit pas dépasser 30 caractères.")
            .When(x => !string.IsNullOrWhiteSpace(x.Telephone));

        RuleFor(x => x.RoleId)
            .GreaterThan(0).WithMessage("Le rôle est obligatoire.");
    }
}

public class ReinitialiserMotDePasseRequeteValidator : AbstractValidator<ReinitialiserMotDePasseRequete>
{
    public ReinitialiserMotDePasseRequeteValidator()
        => RuleFor(x => x.NouveauMotDePasse).SetValidator(new MotDePasseValidator());
}

public class ModifierDroitsRoleRequeteValidator : AbstractValidator<ModifierDroitsRoleRequete>
{
    public ModifierDroitsRoleRequeteValidator()
        => RuleFor(x => x.CodesDroits).NotNull().WithMessage("La liste des droits est obligatoire.");
}
