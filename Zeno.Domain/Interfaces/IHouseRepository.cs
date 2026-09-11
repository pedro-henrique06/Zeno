using HouseEntity = Zeno.Domain.House.House;

namespace Zeno.Domain.Interfaces;

public interface IHouseRepository
{
    Task<HouseEntity?> GetByIdAsync(Guid id);
    Task<IEnumerable<HouseEntity>> GetByUserAsync(Guid userId);
    Task<HouseEntity> CreateAsync(HouseEntity house);
    Task<HouseEntity> UpdateAsync(HouseEntity house);
    Task DeleteAsync(Guid id);
}
