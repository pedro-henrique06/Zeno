using FluentValidation;
using Zeno.Application.Requests.MonthlyExpenseCategories;

namespace Zeno.Application.Validators;

public class CreateMonthlyExpenseCategoryRequestValidator : AbstractValidator<CreateMonthlyExpenseCategoryRequest>
{
    public CreateMonthlyExpenseCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do gasto mensal é obrigatório.")
            .MaximumLength(50).WithMessage("O nome do gasto mensal deve ter no máximo 50 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("O valor não pode ser negativo.");
    }
}

public class UpdateMonthlyExpenseCategoryRequestValidator : AbstractValidator<UpdateMonthlyExpenseCategoryRequest>
{
    public UpdateMonthlyExpenseCategoryRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O Id é obrigatório para atualização.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do gasto mensal é obrigatório.")
            .MaximumLength(50).WithMessage("O nome do gasto mensal deve ter no máximo 50 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("O valor não pode ser negativo.");
    }
}
