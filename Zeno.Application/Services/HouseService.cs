using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Houses;
using Zeno.Domain.Interfaces;
using HouseEntity = Zeno.Domain.House.House;
using EntryEntity = Zeno.Domain.Entry.Entry;

namespace Zeno.Application.Services;

public class HouseService : IHouseService
{
    private readonly IValidator<CreateHouseRequest> _createValidator;
    private readonly IValidator<UpdateHouseRequest> _updateValidator;
    private readonly IHouseRepository _houseRepository;
    private readonly IEntryRepository _entryRepository;

    public HouseService(
        IValidator<CreateHouseRequest> createValidator,
        IValidator<UpdateHouseRequest> updateValidator,
        IHouseRepository houseRepository,
        IEntryRepository entryRepository)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _houseRepository = houseRepository;
        _entryRepository = entryRepository;
    }

    public async Task<IEnumerable<HouseEntity>> GetAllAsync(Guid userId)
    {
        return await _houseRepository.GetByUserAsync(userId);
    }

    public async Task<HouseEntity?> GetByIdAsync(Guid userId, Guid id)
    {
        var house = await _houseRepository.GetByIdAsync(id);
        return house is not null && house.UserId == userId ? house : null;
    }

    public async Task<HouseEntity> CreateAsync(Guid userId, CreateHouseRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var house = new HouseEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        await _houseRepository.CreateAsync(house);
        return house;
    }

    public async Task UpdateAsync(Guid userId, UpdateHouseRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var existing = await _houseRepository.GetByIdAsync(request.Id);
        if (existing is null || existing.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Id), "Casa não encontrada.")
                }));

        existing.Name = request.Name;
        existing.Description = request.Description ?? string.Empty;
        await _houseRepository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var existing = await _houseRepository.GetByIdAsync(id);
        if (existing is null || existing.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Casa não encontrada.")
                }));

        await _houseRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<EntryEntity>> GetEntriesAsync(Guid userId, Guid houseId)
    {
        var house = await _houseRepository.GetByIdAsync(houseId);
        if (house is null || house.UserId != userId)
            return Enumerable.Empty<EntryEntity>();

        return await _entryRepository.GetRecurringByHouseAsync(userId, houseId);
    }
}
