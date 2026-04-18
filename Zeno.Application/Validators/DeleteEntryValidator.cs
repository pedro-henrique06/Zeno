using FluentValidation;
using Zeno.Domain.Entry;

namespace Zeno.Application.Validators;

public class DeleteEntryValidator : AbstractValidator<Entry>
{
    public DeleteEntryValidator()
    {
        RuleFor(x => x.Id)
            .NotNull().WithMessage("O Id é obrigatório para exclusão.");
    }
}
