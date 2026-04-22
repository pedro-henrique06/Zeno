using UserEntity = Zeno.Domain.User.User;

namespace Zeno.Domain.Interfaces;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(Guid id);
    Task<UserEntity?> GetByEmailAsync(string email);
    Task<UserEntity> CreateAsync(UserEntity user);
}
