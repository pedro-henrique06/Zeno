using FluentValidation;
using FluentValidation.Results;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Houses;
using Zeno.Domain.House;
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
    private readonly IUserRepository _userRepository;

    public HouseService(
        IValidator<CreateHouseRequest> createValidator,
        IValidator<UpdateHouseRequest> updateValidator,
        IHouseRepository houseRepository,
        IEntryRepository entryRepository,
        IUserRepository userRepository)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _houseRepository = houseRepository;
        _entryRepository = entryRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<HouseEntity>> GetAllAsync(Guid userId)
    {
        return await _houseRepository.GetByUserAsync(userId);
    }

    public async Task<HouseEntity?> GetByIdAsync(Guid userId, Guid id)
    {
        var house = await _houseRepository.GetByIdAsync(id);
        if (house is null) return null;
        bool canAccess = house.UserId == userId || house.Members.Any(m => m.UserId == userId);
        return canAccess ? house : null;
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
            throw new AppValidationException(new ValidationResult(
                new List<ValidationFailure>
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
            throw new AppValidationException(new ValidationResult(
                new List<ValidationFailure>
                {
                    new(nameof(id), "Casa não encontrada.")
                }));

        await _houseRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<EntryEntity>> GetEntriesAsync(Guid userId, Guid houseId)
    {
        var house = await _houseRepository.GetByIdAsync(houseId);
        if (house is null) return Enumerable.Empty<EntryEntity>();

        bool isOwner = house.UserId == userId;
        bool isMember = house.Members.Any(m => m.UserId == userId);
        if (!isOwner && !isMember) return Enumerable.Empty<EntryEntity>();

        // Always query entries using the owner's userId
        return await _entryRepository.GetRecurringByHouseAsync(house.UserId, houseId);
    }

    public async Task AddMemberAsync(Guid requestingUserId, Guid houseId, string email)
    {
        var house = await _houseRepository.GetByIdAsync(houseId);
        if (house is null || house.UserId != requestingUserId)
            throw new AppValidationException(new ValidationResult(
                new List<ValidationFailure> { new("houseId", "Casa não encontrada.") }));

        var targetUser = await _userRepository.GetByEmailAsync(email.Trim().ToLower());
        if (targetUser is null)
            throw new AppValidationException(new ValidationResult(
                new List<ValidationFailure> { new("email", "Usuário não encontrado com este e-mail.") }));

        if (targetUser.Id == requestingUserId)
            throw new AppValidationException(new ValidationResult(
                new List<ValidationFailure> { new("email", "Você já é o proprietário desta casa.") }));

        if (house.Members.Any(m => m.UserId == targetUser.Id))
            throw new AppValidationException(new ValidationResult(
                new List<ValidationFailure> { new("email", "Este usuário já é membro desta casa.") }));

        var member = new HouseMember
        {
            UserId = targetUser.Id,
            Email = targetUser.Email,
            Name = targetUser.Name,
            JoinedAt = DateTime.UtcNow
        };

        await _houseRepository.AddMemberAsync(houseId, member);
    }

    public async Task RemoveMemberAsync(Guid requestingUserId, Guid houseId, Guid memberId)
    {
        var house = await _houseRepository.GetByIdAsync(houseId);
        if (house is null) return;

        // Owner can remove anyone; a member can only remove themselves
        bool isOwner = house.UserId == requestingUserId;
        bool isSelf = requestingUserId == memberId;

        if (!isOwner && !isSelf)
            throw new AppValidationException(new ValidationResult(
                new List<ValidationFailure> { new("memberId", "Sem permissão para remover este membro.") }));

        await _houseRepository.RemoveMemberAsync(houseId, memberId);
    }
}
