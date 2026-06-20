using Zeno.Application.Requests;
using Zeno.Application.Responses;

namespace Zeno.Application.Interfaces;

public interface IUserService
{
    Task<UserProfileResponse> GetProfile(Guid userId);
    Task<UserProfileResponse> UpdateProfile(Guid userId, UpdateProfileRequest request);
    Task ChangePassword(Guid userId, ChangePasswordRequest request);
}
