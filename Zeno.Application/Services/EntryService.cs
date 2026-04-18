using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Validators;
using Zeno.Domain.Entry;

namespace Zeno.Application.Services;

public class EntryService : IEntryService
{
    private readonly IEnumerable<IValidator<object>> _validators;

    public EntryService(IEnumerable<IValidator<object>> validators)
    {
        _validators = validators;
    }

    public async Task<Entry> CreateEntry(Entry entry)
    {
        await ValidateAsync<EntryValidator, Entry>(entry);

        entry.Id = Guid.NewGuid();

        return entry;
    }

    public async Task<Entry> UpdateEntry(Entry entry)
    {
        await ValidateAsync<UpdateEntryValidator, Entry>(entry);

        return entry;
    }

    public async Task<Entry> DeleteEntry(Guid id)
    {
        var entry = new Entry { Id = id };

        await ValidateAsync<DeleteEntryValidator, Entry>(entry);

        return entry;
    }

    public async Task<IEnumerable<Entry>> GetEntriesByMonth(GetEntriesByMonthQuery query)
    {
        await ValidateAsync<GetEntriesByMonthQueryValidator, GetEntriesByMonthQuery>(query);

        return Enumerable.Empty<Entry>();
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = _validators.OfType<TValidator>().First();
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}
