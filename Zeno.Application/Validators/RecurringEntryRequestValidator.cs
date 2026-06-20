using FluentValidation;
using Zeno.Application.Requests;

namespace Zeno.Application.Validators;

public class CreateRecurringEntryRequestValidator : AbstractValidator<CreateRecurringEntryRequest>
{
    public CreateRecurringEntryRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(200).WithMessage("O título deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo de entrada é inválido.");

        RuleFor(x => x.Kind)
            .IsInEnum().WithMessage("O tipo de lançamento é inválido.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("A categoria é inválida.");

        RuleFor(x => x.DayOfMonth)
            .InclusiveBetween(1, 31).WithMessage("O dia do mês deve estar entre 1 e 31.");

        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("A carteira é obrigatória.");
    }
}

public class UpdateRecurringEntryRequestValidator : AbstractValidator<UpdateRecurringEntryRequest>
{
    public UpdateRecurringEntryRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O Id é obrigatório para atualização.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(200).WithMessage("O título deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo de entrada é inválido.");

        RuleFor(x => x.Kind)
            .IsInEnum().WithMessage("O tipo de lançamento é inválido.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("A categoria é inválida.");

        RuleFor(x => x.DayOfMonth)
            .InclusiveBetween(1, 31).WithMessage("O dia do mês deve estar entre 1 e 31.");
    }
}
