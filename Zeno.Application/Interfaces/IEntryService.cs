using Zeno.Application.Requests;
using Zeno.Domain.Entry;

namespace Zeno.Application.Interfaces;

public interface IEntryService
{
    Task<Entry> CreateEntry(Entry entry);
    Task<Entry> UpdateEntry(Entry entry);
    Task<Entry> DeleteEntry(Guid id);
    Task<IEnumerable<Entry>> GetEntriesByMonth(GetEntriesByMonthQuery query);
}