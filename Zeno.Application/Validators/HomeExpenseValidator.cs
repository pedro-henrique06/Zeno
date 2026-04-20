using FluentValidation;
using Zeno.Domain.Home;

namespace Zeno.Application.Validators;

public class HomeExpenseValidator : AbstractValidator<HomeExpense>
{
    public HomeExpenseValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título da despesa é obrigatório.")
            .MaximumLength(100).WithMessage("O título deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Category)
            .NotEqual(Domain.Enum.Category.None).WithMessage("A categoria é obrigatória.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("O mês deve estar entre 1 e 12.");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("O ano deve estar entre 2000 e 2100.");

        RuleFor(x => x.HomeId)
            .NotEqual(Guid.Empty).WithMessage("A casa é obrigatória.");
    }
}
