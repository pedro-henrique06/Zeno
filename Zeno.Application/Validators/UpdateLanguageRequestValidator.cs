using FluentValidation;
using Zeno.Application.Requests;

namespace Zeno.Application.Validators;

public class UpdateLanguageRequestValidator : AbstractValidator<UpdateLanguageRequest>
{
    public UpdateLanguageRequestValidator()
    {
        RuleFor(x => x.Language)
            .IsInEnum().WithMessage("Idioma inválido.");
    }
}
