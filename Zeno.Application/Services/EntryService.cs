using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Validators;
using Zeno.Domain.Entry;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class EntryService : IEntryService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEntryRepository _repository;

    public EntryService(IServiceProvider serviceProvider, IEntryRepository repository)
    {
        _serviceProvider = serviceProvider;
        _repository = repository;
    }

    public async Task<Entry> CreateEntry(Entry entry)
    {
        await ValidateAsync<EntryValidator, Entry>(entry);

        entry.Id = Guid.NewGuid();

        return await _repository.CreateAsync(entry);
    }

    public async Task<Entry> UpdateEntry(Entry entry)
    {
        await ValidateAsync<UpdateEntryValidator, Entry>(entry);

        return await _repository.UpdateAsync(entry);
    }

    public async Task<Entry> DeleteEntry(Guid id)
    {
        var entry = new Entry { Id = id };

        await ValidateAsync<DeleteEntryValidator, Entry>(entry);

        await _repository.DeleteAsync(id);

        return entry;
    }

    public async Task<IEnumerable<Entry>> GetEntriesByMonth(GetEntriesByMonthQuery query)
    {
        await ValidateAsync<GetEntriesByMonthQueryValidator, GetEntriesByMonthQuery>(query);

        return await _repository.GetByMonthAsync(query.Month!.Value, query.Year!.Value, query.WalletId!.Value);
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}
