using FluentValidation;
using Zeno.Application.Requests;

namespace Zeno.Application.Validators;

public class YearQueryValidator : AbstractValidator<YearQuery>
{
    public YearQueryValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("O ano deve estar entre 2000 e 2100.");
    }
}
