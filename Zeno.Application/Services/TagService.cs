using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests.Tags;
using Zeno.Domain.Interfaces;
using TagEntity = Zeno.Domain.Tag.Tag;

namespace Zeno.Application.Services;

public class TagService : ITagService
{
    private readonly IValidator<CreateTagRequest> _createValidator;
    private readonly IValidator<UpdateTagRequest> _updateValidator;
    private readonly ITagRepository _tagRepository;

    public TagService(
        IValidator<CreateTagRequest> createValidator,
        IValidator<UpdateTagRequest> updateValidator,
        ITagRepository tagRepository)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _tagRepository = tagRepository;
    }

    public async Task<IEnumerable<TagEntity>> GetAllAsync(Guid userId)
    {
        return await _tagRepository.GetByUserAsync(userId);
    }

    public async Task<TagEntity?> GetByIdAsync(Guid userId, Guid id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        return tag is not null && tag.UserId == userId ? tag : null;
    }

    public async Task<TagEntity> CreateAsync(Guid userId, CreateTagRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var tag = new TagEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        await _tagRepository.CreateAsync(tag);
        return tag;
    }

    public async Task UpdateAsync(Guid userId, UpdateTagRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var existing = await _tagRepository.GetByIdAsync(request.Id);
        if (existing is null || existing.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Id), "Tag não encontrada.")
                }));

        existing.Name = request.Name;
        await _tagRepository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(Guid userId, Guid id)
    {
        var existing = await _tagRepository.GetByIdAsync(id);
        if (existing is null || existing.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(id), "Tag não encontrada.")
                }));

        await _tagRepository.DeleteAsync(id);
    }
}
