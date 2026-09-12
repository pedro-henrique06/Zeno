using MongoDB.Driver;
using Zeno.Domain.House;
using Zeno.Domain.Interfaces;
using Zeno.Infrastructure.SQL.Context;
using HouseEntity = Zeno.Domain.House.House;

namespace Zeno.Infrastructure.SQL.Repositories;

public class HouseRepository : IHouseRepository
{
    private readonly ZenoMongoContext _context;

    public HouseRepository(ZenoMongoContext context)
    {
        _context = context;
    }

    public async Task<HouseEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Houses.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<HouseEntity>> GetByUserAsync(Guid userId)
    {
        var builder = Builders<HouseEntity>.Filter;
        var filter = builder.Eq(x => x.UserId, userId)
                   | builder.ElemMatch(x => x.Members, m => m.UserId == userId);
        return await _context.Houses
            .Find(filter)
            .SortBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<HouseEntity> CreateAsync(HouseEntity house)
    {
        await _context.Houses.InsertOneAsync(house);
        return house;
    }

    public async Task<HouseEntity> UpdateAsync(HouseEntity house)
    {
        var filter = Builders<HouseEntity>.Filter.Eq(x => x.Id, house.Id);
        await _context.Houses.ReplaceOneAsync(filter, house);
        return house;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _context.Houses.DeleteOneAsync(x => x.Id == id);
    }

    public async Task AddMemberAsync(Guid houseId, HouseMember member)
    {
        var filter = Builders<HouseEntity>.Filter.Eq(x => x.Id, houseId);
        var update = Builders<HouseEntity>.Update.Push(x => x.Members, member);
        await _context.Houses.UpdateOneAsync(filter, update);
    }

    public async Task RemoveMemberAsync(Guid houseId, Guid memberId)
    {
        var filter = Builders<HouseEntity>.Filter.Eq(x => x.Id, houseId);
        var update = Builders<HouseEntity>.Update.PullFilter(x => x.Members, m => m.UserId == memberId);
        await _context.Houses.UpdateOneAsync(filter, update);
    }
}
