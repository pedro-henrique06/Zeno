using FluentValidation;
using Zeno.Application.Requests;

namespace Zeno.Application.Validators;

public class AddSalaryRequestValidator : AbstractValidator<AddSalaryRequest>
{
    public AddSalaryRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor do salário deve ser maior que zero.");
    }
}
