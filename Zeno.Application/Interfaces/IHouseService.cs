using Zeno.Application.Requests.Houses;
using HouseEntity = Zeno.Domain.House.House;

namespace Zeno.Application.Interfaces;

public interface IHouseService
{
    Task<IEnumerable<HouseEntity>> GetAllAsync(Guid userId);
    Task<HouseEntity?> GetByIdAsync(Guid userId, Guid id);
    Task<HouseEntity> CreateAsync(Guid userId, CreateHouseRequest request);
    Task UpdateAsync(Guid userId, UpdateHouseRequest request);
    Task DeleteAsync(Guid userId, Guid id);
}
