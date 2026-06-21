using FluentValidation;
using Zeno.Application.Requests.Tags;

namespace Zeno.Application.Validators;

public class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da tag é obrigatório.")
            .MaximumLength(50).WithMessage("O nome da tag deve ter no máximo 50 caracteres.");
    }
}

public class UpdateTagRequestValidator : AbstractValidator<UpdateTagRequest>
{
    public UpdateTagRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O Id é obrigatório para atualização.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da tag é obrigatório.")
            .MaximumLength(50).WithMessage("O nome da tag deve ter no máximo 50 caracteres.");
    }
}
