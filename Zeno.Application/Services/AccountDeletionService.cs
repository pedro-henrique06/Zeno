using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class AccountDeletionService : IAccountDeletionService
{
    private readonly IUserService _userService;
    private readonly IUserRepository _userRepository;
    private readonly ICaptureKeyRepository _captureKeyRepository;
    private readonly IWidgetKeyRepository _widgetKeyRepository;
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly IPushSubscriptionRepository _pushSubscriptionRepository;
    private readonly INotificationPreferenceRepository _notificationPreferenceRepository;
    private readonly IHouseRepository _houseRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AccountDeletionService(
        IUserService userService,
        IUserRepository userRepository,
        ICaptureKeyRepository captureKeyRepository,
        IWidgetKeyRepository widgetKeyRepository,
        IDeviceTokenRepository deviceTokenRepository,
        IPushSubscriptionRepository pushSubscriptionRepository,
        INotificationPreferenceRepository notificationPreferenceRepository,
        IHouseRepository houseRepository,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userService = userService;
        _userRepository = userRepository;
        _captureKeyRepository = captureKeyRepository;
        _widgetKeyRepository = widgetKeyRepository;
        _deviceTokenRepository = deviceTokenRepository;
        _pushSubscriptionRepository = pushSubscriptionRepository;
        _notificationPreferenceRepository = notificationPreferenceRepository;
        _houseRepository = houseRepository;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task DeleteAccountAsync(Guid userId)
    {
        _ = await _userRepository.GetByIdAsync(userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("UserId", "Usuário não encontrado.")
                }));

        // Entries, tags, daily-budget items, goal and capture rules.
        await _userService.ResetAccount(userId);

        await _captureKeyRepository.DeleteByUserAsync(userId);
        await _widgetKeyRepository.DeleteByUserAsync(userId);
        await _deviceTokenRepository.DeleteByUserAsync(userId);
        await _pushSubscriptionRepository.DeleteByUserAsync(userId);
        await _notificationPreferenceRepository.DeleteByUserAsync(userId);
        await _houseRepository.RemoveMemberFromAllAsync(userId);
        await _houseRepository.DeleteByOwnerAsync(userId);
        await _refreshTokenRepository.DeleteByUserAsync(userId);

        // Last, so a failure above leaves an account the user can still sign in to and retry.
        await _userRepository.DeleteAsync(userId);
    }
}
