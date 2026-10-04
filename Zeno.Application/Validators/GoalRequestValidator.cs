using FluentValidation;
using Zeno.Application.Requests.Goals;

namespace Zeno.Application.Validators;

public class SaveGoalRequestValidator : AbstractValidator<SaveGoalRequest>
{
    private const decimal MaxAmount = 1_000_000_000_000m;

    public SaveGoalRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da meta é obrigatório.")
            .MaximumLength(60).WithMessage("O nome da meta deve ter no máximo 60 caracteres.");

        RuleFor(x => x.TargetAmount)
            .GreaterThan(0).WithMessage("O valor da meta deve ser maior que zero.")
            .LessThanOrEqualTo(MaxAmount).WithMessage("O valor da meta é muito alto.");

        RuleFor(x => x.MonthlyContribution)
            .GreaterThan(0).WithMessage("O aporte mensal deve ser maior que zero.")
            .LessThanOrEqualTo(MaxAmount).WithMessage("O aporte mensal é muito alto.");

        RuleFor(x => x.InitialAmount)
            .GreaterThanOrEqualTo(0).WithMessage("O valor já guardado não pode ser negativo.")
            .LessThanOrEqualTo(MaxAmount).WithMessage("O valor já guardado é muito alto.");

        RuleFor(x => x.AnnualRatePercent)
            .InclusiveBetween(0, 100).WithMessage("A taxa de juros anual deve estar entre 0% e 100%.");
    }
}
