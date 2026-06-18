using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Homes;
using Zeno.Application.Validators;
using Zeno.Domain.Enum;
using Zeno.Domain.Home;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class HomeService : Zeno.Application.Interfaces.IHomeService
{
    private readonly IValidator<CreateHomeRequest> _createValidator;
    private readonly IValidator<UpdateHomeRequest> _updateValidator;
    private readonly IHomeRepository _repository;

    public HomeService(
        IValidator<CreateHomeRequest> createValidator,
        IValidator<UpdateHomeRequest> updateValidator,
        IHomeRepository repository)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _repository = repository;
    }

    public async Task<Home> CreateHome(Guid userId, CreateHomeRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var home = new Home
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            SplitMode = request.SplitMode,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(home);
        await _repository.AddMemberAsync(home.Id!.Value, userId, (int)HomeRole.Admin);
        return created;
    }

    public async Task<Home> UpdateHome(Guid userId, UpdateHomeRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var isAdmin = await _repository.IsAdminAsync(request.Id, userId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Id), "Apenas o administrador pode atualizar a casa.")
                }));

        var existing = await _repository.GetByIdAsync(request.Id);
        if (existing is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Id), "Casa não encontrada.")
                }));

        existing.Name = request.Name;
        existing.Description = request.Description ?? existing.Description;
        existing.SplitMode = request.SplitMode;

        return await _repository.UpdateAsync(existing);
    }

    public async Task<Home> DeleteHome(Guid userId, Guid id)
    {
        var isAdmin = await _repository.IsAdminAsync(id, userId);
        if (!isAdmin)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Apenas o administrador pode excluir a casa.")
                }));

        var home = await _repository.GetByIdAsync(id);
        if (home is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Casa não encontrada.")
                }));

        await _repository.DeleteAsync(id);
        return home;
    }

    public async Task<Home?> GetHomeById(Guid userId, Guid id)
    {
        return await _repository.GetByIdAndMemberAsync(id, userId);
    }

    public async Task<IEnumerable<Home>> GetAllHomes(Guid userId)
    {
        return await _repository.GetAllByUserAsync(userId);
    }
}