using FluentValidation;
using Zeno.Domain.Entry;

namespace Zeno.Application.Validators;

public class EntryValidator : AbstractValidator<Entry>
{
    public EntryValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(100).WithMessage("O título deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo de entrada é inválido.");

        RuleFor(x => x.Category)
            .NotEqual(Domain.Enum.Category.None).WithMessage("A categoria é obrigatória.");
    }
}
