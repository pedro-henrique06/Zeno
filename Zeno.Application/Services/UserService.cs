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

    public UserService(IServiceProvider serviceProvider, IUserRepository userRepository)
    {
        _serviceProvider = serviceProvider;
        _userRepository = userRepository;
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
            HasPassword = !string.IsNullOrEmpty(user.PasswordHash)
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
