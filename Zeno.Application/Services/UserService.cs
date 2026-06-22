using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Application.Responses;
using Zeno.Application.Validators;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class UserService : IUserService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserRepository _userRepository;
    private readonly IEntryRepository _entryRepository;
    private readonly IMonthlyExpenseCategoryRepository _monthlyExpenseCategoryRepository;
    private readonly IExchangeRateService _exchangeRateService;

    public UserService(
        IServiceProvider serviceProvider,
        IUserRepository userRepository,
        IEntryRepository entryRepository,
        IMonthlyExpenseCategoryRepository monthlyExpenseCategoryRepository,
        IExchangeRateService exchangeRateService)
    {
        _serviceProvider = serviceProvider;
        _userRepository = userRepository;
        _entryRepository = entryRepository;
        _monthlyExpenseCategoryRepository = monthlyExpenseCategoryRepository;
        _exchangeRateService = exchangeRateService;
    }

    public async Task<UserProfileResponse> GetProfile(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("UserId", "Usuário não encontrado.")
                }));

        return ToResponse(user);
    }

    public async Task<UserProfileResponse> UpdateProfile(Guid userId, UpdateProfileRequest request)
    {
        await ValidateAsync<UpdateProfileRequestValidator, UpdateProfileRequest>(request);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("UserId", "Usuário não encontrado.")
                }));

        if (await _userRepository.EmailExistsForOtherUserAsync(request.Email, userId))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("Email", "Este e-mail já está em uso.")
                }));

        user.Name = request.Name;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.Document = request.Document;
        user.BirthDate = request.BirthDate;

        await _userRepository.UpdateProfileAsync(user);

        return ToResponse(user);
    }

    public async Task ChangePassword(Guid userId, ChangePasswordRequest request)
    {
        await ValidateAsync<ChangePasswordRequestValidator, ChangePasswordRequest>(request);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("UserId", "Usuário não encontrado.")
                }));

        if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("CurrentPassword", "Senha atual inválida.")
                }));

        var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdatePasswordAsync(userId, newHash);
    }

    public async Task<UserProfileResponse> UpdateDailyBudget(Guid userId, UpdateDailyBudgetRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("UserId", "Usuário não encontrado.")
                }));

        if (request.DailyBudget < 0)
            throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("DailyBudget", "A previsão de diário não pode ser negativa.")
                }));

        user.DailyBudget = request.DailyBudget;
        await _userRepository.UpdateProfileAsync(user);

        return ToResponse(user);
    }

    public async Task<UserProfileResponse> UpdateCurrency(Guid userId, UpdateCurrencyRequest request)
    {
        await ValidateAsync<UpdateCurrencyRequestValidator, UpdateCurrencyRequest>(request);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("UserId", "Usuário não encontrado.")
                }));

        if (user.Currency != request.Currency)
        {
            var rate = await _exchangeRateService.GetRateAsync(user.Currency, request.Currency);

            if (user.DailyBudget.HasValue)
                user.DailyBudget = Math.Round(user.DailyBudget.Value * rate, 2);

            user.Currency = request.Currency;

            await _entryRepository.MultiplyValuesForUserAsync(userId, rate);
            await _monthlyExpenseCategoryRepository.MultiplyAmountsForUserAsync(userId, rate);
            await _userRepository.UpdateProfileAsync(user);
        }

        return ToResponse(user);
    }

    public async Task<UserProfileResponse> UpdateLanguage(Guid userId, UpdateLanguageRequest request)
    {
        await ValidateAsync<UpdateLanguageRequestValidator, UpdateLanguageRequest>(request);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new AppValidationException(new FluentValidation.Results.ValidationResult(
                new List<FluentValidation.Results.ValidationFailure>
                {
                    new("UserId", "Usuário não encontrado.")
                }));

        user.Language = request.Language;
        await _userRepository.UpdateProfileAsync(user);

        return ToResponse(user);
    }

    private static UserProfileResponse ToResponse(Zeno.Domain.User.User user)
    {
        return new UserProfileResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Document = user.Document,
            BirthDate = user.BirthDate,
            OAuthProvider = user.Provider.ToString(),
            HasPassword = !string.IsNullOrEmpty(user.PasswordHash),
            DailyBudget = user.DailyBudget,
            Currency = user.Currency.ToString(),
            Language = user.Language.ToString()
        };
    }

    private async Task ValidateAsync<TValidator, T>(T instance) where TValidator : IValidator<T>
    {
        var validator = (TValidator)_serviceProvider.GetService(typeof(TValidator))!;
        var result = await validator.ValidateAsync(instance!);

        if (!result.IsValid)
            throw new AppValidationException(result);
    }
}
