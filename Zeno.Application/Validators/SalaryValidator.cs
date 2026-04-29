using FluentValidation;
using Zeno.Domain.Salary;

namespace Zeno.Application.Validators;

public class SalaryValidator : AbstractValidator<Salary>
{
    public SalaryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("O usuário é obrigatório.");

        RuleFor(x => x.AccountId)
            .NotEqual(Guid.Empty).WithMessage("A conta é obrigatória.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor do salário deve ser maior que zero.");

        RuleFor(x => x.DayOfMonth)
            .InclusiveBetween(1, 31).WithMessage("O dia do mês deve estar entre 1 e 31.");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("A descrição deve ter no máximo 200 caracteres.");
    }
}
