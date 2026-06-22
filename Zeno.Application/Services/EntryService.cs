using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Requests.Entries;
using Zeno.Application.Responses.Common;
using Zeno.Domain.Entry;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class EntryService : IEntryService
{
    private readonly IValidator<CreateEntryRequest> _createValidator;
    private readonly IValidator<UpdateEntryRequest> _updateValidator;
    private readonly IValidator<DeleteEntryRequest> _deleteValidator;
    private readonly IValidator<GetEntriesByMonthQuery> _getEntriesValidator;
    private readonly IEntryRepository _entryRepository;
    private readonly ITagRepository _tagRepository;

    public EntryService(
        IValidator<CreateEntryRequest> createValidator,
        IValidator<UpdateEntryRequest> updateValidator,
        IValidator<DeleteEntryRequest> deleteValidator,
        IValidator<GetEntriesByMonthQuery> getEntriesValidator,
        IEntryRepository entryRepository,
        ITagRepository tagRepository)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _deleteValidator = deleteValidator;
        _getEntriesValidator = getEntriesValidator;
        _entryRepository = entryRepository;
        _tagRepository = tagRepository;
    }

    private async Task EnsureTagOwnershipAsync(Guid userId, Guid? tagId, string propertyName)
    {
        if (tagId is null)
            return;

        var tag = await _tagRepository.GetByIdAsync(tagId.Value);
        if (tag is null || tag.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(propertyName, "Tag não encontrada.")
                }));
    }

    public async Task<PagedResponse<Entry>> GetEntriesByMonth(Guid userId, GetEntriesByMonthQuery query)
    {
        var validation = await _getEntriesValidator.ValidateAsync(query);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var pageSize = Math.Min(query.PageSize, 100);

        (var items, int totalCount) = await _entryRepository.GetByMonthForUserPagedAsync(
            query.Month!.Value,
            query.Year!.Value,
            userId,
            query.Page,
            pageSize);

        return new PagedResponse<Entry>
        {
            Items = items.ToList(),
            Page = query.Page,
            PageSize = pageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Entry> CreateEntry(Guid userId, CreateEntryRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        await EnsureTagOwnershipAsync(userId, request.TagId, nameof(request.TagId));

        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            Value = request.Value,
            Kind = request.Kind,
            Description = request.Description ?? string.Empty,
            TagId = request.TagId,
            Date = request.Date,
            IsRecurring = request.IsRecurring
        };

        await _entryRepository.CreateAsync(entry);

        return entry;
    }

    public async Task<Entry> UpdateEntry(Guid userId, UpdateEntryRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var existing = await _entryRepository.GetByIdAsync(request.Id);
        if (existing is null || existing.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Id), "Lançamento não encontrado.")
                }));

        await EnsureTagOwnershipAsync(userId, request.TagId, nameof(request.TagId));

        var updatedEntry = new Entry
        {
            Id = request.Id,
            UserId = userId,
            Title = request.Title,
            Value = request.Value,
            Kind = request.Kind,
            Description = request.Description ?? string.Empty,
            TagId = request.TagId,
            Date = request.Date,
            IsRecurring = request.IsRecurring
        };

        await _entryRepository.UpdateAsync(updatedEntry);
        return updatedEntry;
    }

    public async Task<Entry> DeleteEntry(Guid userId, DeleteEntryRequest request)
    {
        var validation = await _deleteValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var existing = await _entryRepository.GetByIdAsync(request.Id);
        if (existing is null || existing.UserId != userId)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Id), "Lançamento não encontrado.")
                }));

        await _entryRepository.DeleteAsync(request.Id);
        return existing;
    }
}
