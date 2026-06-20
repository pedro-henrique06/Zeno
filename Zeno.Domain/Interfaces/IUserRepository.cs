using UserEntity = Zeno.Domain.User.User;

namespace Zeno.Domain.Interfaces;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(Guid id);
    Task<UserEntity?> GetByEmailAsync(string email);
    Task<UserEntity?> GetByProviderAsync(string provider, string providerId);
    Task<UserEntity> CreateAsync(UserEntity user);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> EmailExistsForOtherUserAsync(string email, Guid userId);
    Task<UserEntity> UpdateProfileAsync(UserEntity user);
    Task UpdatePasswordAsync(Guid userId, string passwordHash);
}
