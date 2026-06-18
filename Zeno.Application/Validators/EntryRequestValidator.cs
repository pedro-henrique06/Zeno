using FluentValidation;
using Zeno.Application.Requests.Entries;

namespace Zeno.Application.Validators;

public class CreateEntryRequestValidator : AbstractValidator<CreateEntryRequest>
{
    public CreateEntryRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(100).WithMessage("O título deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo de entrada é inválido.");

        RuleFor(x => x.Category)
            .NotEqual(Domain.Enum.Category.None).WithMessage("A categoria é obrigatória.");

        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("A carteira é obrigatória.");
    }
}

public class UpdateEntryRequestValidator : AbstractValidator<UpdateEntryRequest>
{
    public UpdateEntryRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O Id é obrigatório para atualização.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(100).WithMessage("O título deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo de entrada é inválido.");

        RuleFor(x => x.Category)
            .NotEqual(Domain.Enum.Category.None).WithMessage("A categoria é obrigatória.");

        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("A carteira é obrigatória.");
    }
}

public class DeleteEntryRequestValidator : AbstractValidator<DeleteEntryRequest>
{
    public DeleteEntryRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O Id é obrigatório para exclusão.");
    }
}