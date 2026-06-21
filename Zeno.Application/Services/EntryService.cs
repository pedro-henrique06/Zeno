using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Requests.Entries;
using Zeno.Application.Responses.Common;
using Zeno.Application.Validators;
using Zeno.Domain.Entry;
using Zeno.Domain.Enum;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class EntryService : IEntryService
{
    private readonly IValidator<CreateEntryRequest> _createValidator;
    private readonly IValidator<UpdateEntryRequest> _updateValidator;
    private readonly IValidator<DeleteEntryRequest> _deleteValidator;
    private readonly IValidator<GetEntriesByMonthQuery> _getEntriesValidator;
    private readonly IEntryRepository _entryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryRuleService _categoryRuleService;

    public EntryService(
        IValidator<CreateEntryRequest> createValidator,
        IValidator<UpdateEntryRequest> updateValidator,
        IValidator<DeleteEntryRequest> deleteValidator,
        IValidator<GetEntriesByMonthQuery> getEntriesValidator,
        IEntryRepository entryRepository,
        IUnitOfWork unitOfWork,
        ICategoryRuleService categoryRuleService)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _deleteValidator = deleteValidator;
        _getEntriesValidator = getEntriesValidator;
        _entryRepository = entryRepository;
        _unitOfWork = unitOfWork;
        _categoryRuleService = categoryRuleService;
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

        Guid? categoryId = request.CategoryId;
        if (!categoryId.HasValue && request.Category == Category.None && !string.IsNullOrWhiteSpace(request.Description))
        {
            var matchedCategory = await _categoryRuleService.ApplyRuleAsync(userId, request.Description);
            if (matchedCategory is not null)
                categoryId = matchedCategory.Id;
        }

        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Value = request.Value,
            Type = request.Type,
            Kind = request.Kind,
            Description = request.Description ?? string.Empty,
            Category = request.Category,
            CategoryId = categoryId,
            Date = request.Date,
            WalletId = request.WalletId
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
        if (existing is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Id), "Lançamento não encontrado.")
                }));

        var updatedEntry = new Entry
        {
            Id = request.Id,
            Title = request.Title,
            Value = request.Value,
            Type = request.Type,
            Kind = request.Kind,
            Description = request.Description ?? string.Empty,
            Category = request.Category,
            CategoryId = request.CategoryId,
            Date = request.Date,
            WalletId = request.WalletId
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
        if (existing is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Id), "Lançamento não encontrado.")
                }));

        await _entryRepository.DeleteAsync(request.Id);
        return existing;
    }
}