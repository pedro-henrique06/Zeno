using FluentValidation;
using Zeno.Domain.Home;

namespace Zeno.Application.Validators;

public class HomeValidator : AbstractValidator<Home>
{
    public HomeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da casa é obrigatório.")
            .MaximumLength(50).WithMessage("O nome deve ter no máximo 50 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("A descrição deve ter no máximo 200 caracteres.");
    }
}
