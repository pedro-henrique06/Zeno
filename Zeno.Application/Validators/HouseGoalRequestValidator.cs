using FluentValidation;
using Zeno.Application.Requests.Houses;

namespace Zeno.Application.Validators;

public class SaveHouseGoalRequestValidator : AbstractValidator<SaveHouseGoalRequest>
{
    public SaveHouseGoalRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da meta é obrigatório.")
            .MaximumLength(60).WithMessage("O nome da meta deve ter no máximo 60 caracteres.");

        RuleFor(x => x.TargetAmount)
            .GreaterThan(0).WithMessage("O valor da meta deve ser maior que zero.")
            .LessThanOrEqualTo(1_000_000_000_000m).WithMessage("O valor da meta é muito alto.");
    }
}
