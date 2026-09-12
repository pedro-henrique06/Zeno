using FluentValidation;
using Zeno.Application.Requests;

namespace Zeno.Application.Validators;

public class UpdateCurrencyRequestValidator : AbstractValidator<UpdateCurrencyRequest>
{
    public UpdateCurrencyRequestValidator()
    {
        RuleFor(x => x.Currency)
            .IsInEnum().WithMessage("Moeda inválida.");
    }
}
