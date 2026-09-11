using FluentValidation;
using Zeno.Application.Requests.Houses;

namespace Zeno.Application.Validators;

public class CreateHouseRequestValidator : AbstractValidator<CreateHouseRequest>
{
    public CreateHouseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da casa é obrigatório.")
            .MaximumLength(100).WithMessage("O nome da casa deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres.")
            .When(x => x.Description is not null);
    }
}

public class UpdateHouseRequestValidator : AbstractValidator<UpdateHouseRequest>
{
    public UpdateHouseRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O Id é obrigatório para atualização.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da casa é obrigatório.")
            .MaximumLength(100).WithMessage("O nome da casa deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres.")
            .When(x => x.Description is not null);
    }
}
