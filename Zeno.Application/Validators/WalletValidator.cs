using FluentValidation;
using Zeno.Domain.Wallet;

namespace Zeno.Application.Validators;

public class WalletValidator : AbstractValidator<Wallet>
{
    public WalletValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("O usuário é obrigatório.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da carteira é obrigatório.")
            .MaximumLength(50).WithMessage("O nome deve ter no máximo 50 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("A descrição deve ter no máximo 200 caracteres.");
    }
}
