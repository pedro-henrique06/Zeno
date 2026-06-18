using FluentValidation;
using Zeno.Application.Requests.Homes;

namespace Zeno.Application.Validators;

public class CreateHomeRequestValidator : AbstractValidator<CreateHomeRequest>
{
    public CreateHomeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da casa é obrigatório.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.SplitMode)
            .IsInEnum().WithMessage("O modo de divisão é inválido.");
    }
}

public class UpdateHomeRequestValidator : AbstractValidator<UpdateHomeRequest>
{
    public UpdateHomeRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O Id é obrigatório.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da casa é obrigatório.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.SplitMode)
            .IsInEnum().WithMessage("O modo de divisão é inválido.");
    }
}

public class CreateHomeExpenseRequestValidator : AbstractValidator<CreateHomeExpenseRequest>
{
    public CreateHomeExpenseRequestValidator()
    {
        RuleFor(x => x.HomeId)
            .NotEmpty().WithMessage("O Id da casa é obrigatório.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(100).WithMessage("O título deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Category)
            .NotEqual(Domain.Enum.Category.None).WithMessage("A categoria é obrigatória.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("O mês deve estar entre 1 e 12.");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("O ano deve estar entre 2000 e 2100.");
    }
}