using FluentValidation;
using Zeno.Domain.Entry;

namespace Zeno.Application.Validators;

public class UpdateEntryValidator : AbstractValidator<Entry>
{
    public UpdateEntryValidator()
    {
        RuleFor(x => x.Id)
            .NotNull().WithMessage("O Id é obrigatório para atualização.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(100).WithMessage("O título deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo de entrada é inválido.");

        RuleFor(x => x.Category)
            .NotEqual(Domain.Enum.Category.None).WithMessage("A categoria é obrigatória.");

        RuleFor(x => x.WalletId)
            .NotNull().WithMessage("A carteira é obrigatória.");
    }
}
