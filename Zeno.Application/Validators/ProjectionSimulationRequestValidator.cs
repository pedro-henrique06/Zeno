using FluentValidation;
using Zeno.Application.Requests;

namespace Zeno.Application.Validators;

public class ProjectionSimulationRequestValidator : AbstractValidator<ProjectionSimulationRequest>
{
    public ProjectionSimulationRequestValidator()
    {
        RuleFor(x => x.WalletId)
            .NotNull().WithMessage("A carteira é obrigatória.");

        RuleFor(x => x.ExtraExpenseAmount)
            .GreaterThanOrEqualTo(0).WithMessage("O valor do gasto extra não pode ser negativo.");

        RuleFor(x => x.MonthsToProject)
            .InclusiveBetween(1, 24).WithMessage("A projeção deve ser entre 1 e 24 meses.");
    }
}
