using Zeno.Application.Requests;
using Zeno.Domain.Entry;

namespace Zeno.Application.Interfaces;

public interface IEntryService
{
    Task<Entry> CreateEntry(Guid userId, Entry entry);
    Task<Entry> UpdateEntry(Guid userId, Entry entry);
    Task<Entry> DeleteEntry(Guid userId, Guid id);
    Task<IEnumerable<Entry>> GetEntriesByMonth(Guid userId, GetEntriesByMonthQuery query);
}