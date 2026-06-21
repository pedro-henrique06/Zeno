using FluentValidation;
using Zeno.Application.Requests;

namespace Zeno.Application.Validators;

public class MonthQueryValidator : AbstractValidator<MonthQuery>
{
    public MonthQueryValidator()
    {
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("O mês deve estar entre 1 e 12.");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("O ano deve estar entre 2000 e 2100.");
    }
}
