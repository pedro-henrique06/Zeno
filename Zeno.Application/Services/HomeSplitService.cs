using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Domain.Home;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class HomeSplitService : Zeno.Application.Interfaces.IHomeSplitService
{
    private readonly IHomeRepository _repository;

    public HomeSplitService(IHomeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ExpenseSplitResult>> CalculateExpenseSplit(Guid userId, Guid homeId, int month, int year)
    {
        var isMember = await _repository.IsMemberAsync(homeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Você não é membro desta casa.")
                }));

        return await _repository.CalculateSplitAsync(homeId, month, year);
    }

    public async Task<IEnumerable<HomeWallet>> GetWallets(Guid userId, Guid homeId)
    {
        var isMember = await _repository.IsMemberAsync(homeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Você não é membro desta casa.")
                }));

        return await _repository.GetWalletsByHomeAsync(homeId);
    }

    public async Task AddWalletToHome(Guid userId, Guid homeId, Guid walletId)
    {
        var isMember = await _repository.IsMemberAsync(homeId, userId);
        if (!isMember)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Você não é membro desta casa.")
                }));

        await _repository.AddWalletAsync(homeId, walletId);
    }

    public async Task RemoveWalletFromHome(Guid adminUserId, Guid homeId, Guid walletId)
    {
        var isAdmin = await _repository.IsAdminAsync(homeId, adminUserId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(homeId), "Apenas o administrador pode remover carteiras.")
                }));

        await _repository.RemoveWalletAsync(homeId, walletId);
    }
}