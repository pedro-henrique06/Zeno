using FluentValidation;
using Zeno.Application.Requests;

namespace Zeno.Application.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("A senha atual é obrigatória.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("A nova senha é obrigatória.")
            .MinimumLength(8).WithMessage("A senha deve ter no mínimo 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("A senha deve conter pelo menos uma letra maiúscula.")
            .Matches(@"[a-z]").WithMessage("A senha deve conter pelo menos uma letra minúscula.")
            .Matches(@"[0-9]").WithMessage("A senha deve conter pelo menos um número.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("A senha deve conter pelo menos um caractere especial.");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("A confirmação da nova senha é obrigatória.")
            .Equal(x => x.NewPassword).WithMessage("As senhas não coincidem.");
    }
}
