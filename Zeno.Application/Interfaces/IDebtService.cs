using Zeno.Application.Requests;
using Zeno.Application.Responses;
using Zeno.Domain.Interfaces;
using Zeno.Domain.Debt;

namespace Zeno.Application.Interfaces;

public interface IDebtService
{
    Task<Debt> CreateAsync(Guid userId, CreateDebtRequest request);
    Task<IEnumerable<Debt>> GetAllAsync(Guid userId);
    Task<Debt?> GetByIdAsync(Guid userId, Guid id);
    Task<Debt> UpdateAsync(Guid userId, UpdateDebtRequest request);
    Task DeleteAsync(Guid userId, Guid id);
    Task<PayoffSimulationResponse> GetPayoffSimulationAsync(Guid userId, Guid id);
    Task<DebtSummaryResponse> GetSummaryAsync(Guid userId);
}