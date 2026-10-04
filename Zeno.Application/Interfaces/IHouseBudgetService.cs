using Zeno.Application.Requests.Houses;
using Zeno.Application.Responses.Houses;

namespace Zeno.Application.Interfaces;

public interface IHouseBudgetService
{
    /// <summary>Orçamento 50/30/20 da casa no mês (null = mês atual). Só dono e moradores.</summary>
    Task<HouseBudgetResponse> GetBudgetAsync(Guid userId, Guid houseId, int? month, int? year);

    /// <summary>Cria ou substitui a meta da casa. Só o dono.</summary>
    Task<HouseGoalResponse?> SaveGoalAsync(Guid userId, Guid houseId, SaveHouseGoalRequest request);

    Task DeleteGoalAsync(Guid userId, Guid houseId);
}
