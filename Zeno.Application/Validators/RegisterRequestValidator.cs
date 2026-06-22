using FluentValidation;
using Zeno.Application.Requests;

namespace Zeno.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MinimumLength(2).WithMessage("O nome deve ter pelo menos 2 caracteres.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("O e-mail é inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("A senha deve conter pelo menos uma letra maiúscula.")
            .Matches(@"[a-z]").WithMessage("A senha deve conter pelo menos uma letra minúscula.")
            .Matches(@"[0-9]").WithMessage("A senha deve conter pelo menos um número.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("A senha deve conter pelo menos um caractere especial.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("A confirmação de senha é obrigatória.")
            .Equal(x => x.Password).WithMessage("As senhas não coincidem.");

        RuleFor(x => x.Phone)
            .Matches(@"^\d{10,11}$").WithMessage("O telefone deve ter 10 ou 11 dígitos.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Document)
            .Matches(@"^\d{11}$|^\d{14}$").WithMessage("O documento deve ser CPF (11 dígitos) ou CNPJ (14 dígitos).")
            .When(x => !string.IsNullOrEmpty(x.Document));

        RuleFor(x => x.BirthDate)
            .LessThan(DateTime.UtcNow.AddYears(-13)).WithMessage("O usuário deve ter pelo menos 13 anos.")
            .When(x => x.BirthDate.HasValue);

        RuleFor(x => x.Currency)
            .IsInEnum().WithMessage("Moeda inválida.")
            .When(x => x.Currency.HasValue);

        RuleFor(x => x.Language)
            .IsInEnum().WithMessage("Idioma inválido.")
            .When(x => x.Language.HasValue);
    }
}