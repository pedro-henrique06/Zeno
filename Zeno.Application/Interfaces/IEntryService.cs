using Zeno.Application.Requests;
using Zeno.Application.Requests.Entries;
using Zeno.Application.Responses.Common;
using Zeno.Domain.Entry;

namespace Zeno.Application.Interfaces;

public interface IEntryService
{
    Task<PagedResponse<Entry>> GetEntriesByMonth(Guid userId, GetEntriesByMonthQuery query);
    Task<Entry> CreateEntry(Guid userId, CreateEntryRequest request);
    Task<Entry> UpdateEntry(Guid userId, UpdateEntryRequest request);
    Task<Entry> DeleteEntry(Guid userId, DeleteEntryRequest request);
}