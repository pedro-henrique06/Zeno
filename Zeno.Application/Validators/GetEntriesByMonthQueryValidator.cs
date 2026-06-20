using FluentValidation;
using Zeno.Application.Requests;

namespace Zeno.Application.Validators;

public class GetEntriesByMonthQueryValidator : AbstractValidator<GetEntriesByMonthQuery>
{
    public GetEntriesByMonthQueryValidator()
    {
        RuleFor(x => x.Month)
            .NotNull().WithMessage("O mês é obrigatório.")
            .InclusiveBetween(1, 12).WithMessage("O mês deve estar entre 1 e 12.");

        RuleFor(x => x.Year)
            .NotNull().WithMessage("O ano é obrigatório.")
            .InclusiveBetween(2000, 2100).WithMessage("O ano deve estar entre 2000 e 2100.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("A página deve ser maior ou igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200).WithMessage("O tamanho da página deve estar entre 1 e 200.");
    }
}
