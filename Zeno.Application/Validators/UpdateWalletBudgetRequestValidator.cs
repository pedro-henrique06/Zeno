using FluentValidation;
using Zeno.Application.Requests;

namespace Zeno.Application.Validators;

public class UpdateWalletBudgetRequestValidator : AbstractValidator<UpdateWalletBudgetRequest>
{
    public UpdateWalletBudgetRequestValidator()
    {
        RuleFor(x => x.DailyBudget)
            .GreaterThanOrEqualTo(0).WithMessage("O orçamento diário deve ser maior ou igual a zero.")
            .When(x => x.DailyBudget.HasValue);
    }
}
