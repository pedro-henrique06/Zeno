using Zeno.Application.Requests;
using Zeno.Application.Responses;

namespace Zeno.Application.Interfaces;

public interface IUserService
{
    Task<UserProfileResponse> GetProfile(Guid userId);
    Task<UserProfileResponse> UpdateProfile(Guid userId, UpdateProfileRequest request);
    Task ChangePassword(Guid userId, ChangePasswordRequest request);
    Task<UserProfileResponse> UpdateDailyBudget(Guid userId, UpdateDailyBudgetRequest request);
    Task<UserProfileResponse> UpdateCurrency(Guid userId, UpdateCurrencyRequest request);
    Task<UserProfileResponse> UpdateLanguage(Guid userId, UpdateLanguageRequest request);

    /// <summary>
    /// Wipes the user's financial data (entries, tags, daily-budget items, personal goal and the capture
    /// rules that point at tags) so they can start over. The account, profile, login, shared houses and
    /// integration keys (widget, Apple Pay capture, notifications) are kept.
    /// </summary>
    Task ResetAccount(Guid userId);
}
