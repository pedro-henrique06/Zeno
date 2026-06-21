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
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICategoryRuleService _categoryRuleService;

    public EntryService(
        IValidator<CreateEntryRequest> createValidator,
        IValidator<UpdateEntryRequest> updateValidator,
        IValidator<DeleteEntryRequest> deleteValidator,
        IValidator<GetEntriesByMonthQuery> getEntriesValidator,
        IEntryRepository entryRepository,
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        ICategoryRuleService categoryRuleService)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _deleteValidator = deleteValidator;
        _getEntriesValidator = getEntriesValidator;
        _entryRepository = entryRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _categoryRuleService = categoryRuleService;
    }

    public async Task<PagedResponse<Entry>> GetEntriesByMonth(Guid userId, GetEntriesByMonthQuery query)
    {
        var validation = await _getEntriesValidator.ValidateAsync(query);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var pageSize = Math.Min(query.PageSize, 100);

        IEnumerable<Entry> items;
        int totalCount;

        if (!query.WalletId.HasValue)
        {
            (items, totalCount) = await _entryRepository.GetByMonthForUserPagedAsync(
                query.Month!.Value,
                query.Year!.Value,
                userId,
                query.Page,
                pageSize);
        }
        else
        {
            var wallet = await _walletRepository.GetByIdAndUserAsync(query.WalletId.Value, userId);
            if (wallet is null)
                throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                    new List<FluentValidation.Results.ValidationFailure>
                    {
                        new("WalletId", "Carteira não encontrada.")
                    }));

            (items, totalCount) = await _entryRepository.GetByMonthPagedAsync(
                query.Month!.Value,
                query.Year!.Value,
                query.WalletId.Value,
                query.Type,
                query.Category,
                query.Page,
                pageSize);
        }

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

        var wallet = await _walletRepository.GetByIdAndUserAsync(request.WalletId, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

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

        await _unitOfWork.BeginAsync();

        try
        {
            await _entryRepository.CreateAsync(entry, _unitOfWork.Transaction);
            await _walletRepository.AddBalanceAsync(request.WalletId, GetBalanceAmount(request.Type, request.Value), _unitOfWork.Transaction);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        return entry;
    }

    public async Task<Entry> UpdateEntry(Guid userId, UpdateEntryRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var oldEntry = await _entryRepository.GetByIdAsync(request.Id);
        if (oldEntry is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new(nameof(request.Id), "Lançamento não encontrado.")
                }));

        var wallet = await _walletRepository.GetByIdAndUserAsync(oldEntry.WalletId, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

        await _unitOfWork.BeginAsync();

        try
        {
            await _walletRepository.AddBalanceAsync(oldEntry.WalletId, GetReverseBalanceAmount(oldEntry.Type, oldEntry.Value), _unitOfWork.Transaction);

            var updatedEntry = new Entry
            {
                Id = request.Id,
                Title = request.Title,
                Value = request.Value,
                Type = request.Type,
                Kind = request.Kind,
                Description = request.Description ?? string.Empty,
                Category = request.Category,
                Date = request.Date,
                WalletId = request.WalletId
            };

            await _entryRepository.UpdateAsync(updatedEntry, _unitOfWork.Transaction);
            await _walletRepository.AddBalanceAsync(request.WalletId, GetBalanceAmount(request.Type, request.Value), _unitOfWork.Transaction);

            await _unitOfWork.CommitAsync();
            return updatedEntry;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
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

        var wallet = await _walletRepository.GetByIdAndUserAsync(existing.WalletId, userId);
        if (wallet is null)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("WalletId", "Carteira não encontrada.")
                }));

        await _unitOfWork.BeginAsync();

        try
        {
            await _walletRepository.AddBalanceAsync(existing.WalletId, GetReverseBalanceAmount(existing.Type, existing.Value), _unitOfWork.Transaction);
            await _entryRepository.DeleteAsync(request.Id, _unitOfWork.Transaction);
            await _unitOfWork.CommitAsync();
            return existing;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private static decimal GetBalanceAmount(EntryType type, decimal value)
    {
        return type == EntryType.Credit ? value : -value;
    }

    private static decimal GetReverseBalanceAmount(EntryType type, decimal value)
    {
        return type == EntryType.Credit ? -value : value;
    }
}