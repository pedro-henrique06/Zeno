using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Zeno.Application.Exceptions;
using Zeno.Application.Interfaces;
using Zeno.Application.Requests;
using Zeno.Domain.Auth;
using Zeno.Domain.Interfaces;

namespace Zeno.Application.Services;

public class PasswordResetService : IPasswordResetService
{
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    public const int MaxAttempts = 5;

    private readonly IValidator<ResetPasswordRequest> _validator;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetCodeRepository _codeRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;

    public PasswordResetService(
        IValidator<ResetPasswordRequest> validator,
        IUserRepository userRepository,
        IPasswordResetCodeRepository codeRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IEmailSender emailSender,
        IClock clock)
    {
        _validator = validator;
        _userRepository = userRepository;
        _codeRepository = codeRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _emailSender = emailSender;
        _clock = clock;
    }

    public async Task RequestAsync(ForgotPasswordRequest request)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        if (email.Length == 0) return;

        // Google/Apple-only accounts have no password to reset; unknown emails get the same silence.
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash)) return;

        var now = _clock.UtcNow;
        var previous = await _codeRepository.GetByUserAsync(user.Id);
        if (previous is not null && now - previous.CreatedAt < ResendCooldown) return;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        await _codeRepository.ReplaceAsync(new PasswordResetCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = Hash(user.Id, code),
            ExpiresAt = now.Add(CodeLifetime),
            CreatedAt = now,
        });

        var minutes = (int)CodeLifetime.TotalMinutes;
        await _emailSender.SendAsync(
            user.Email,
            $"Seu código do Zeno: {code}",
            $"""
             <div style="font-family:-apple-system,Segoe UI,Roboto,sans-serif;font-size:16px;color:#111">
               <p>Olá, {System.Net.WebUtility.HtmlEncode(user.Name)}.</p>
               <p>Use este código para criar uma nova senha no Zeno:</p>
               <p style="font-size:32px;font-weight:700;letter-spacing:6px">{code}</p>
               <p>Ele vale por {minutes} minutos. Se não foi você que pediu, ignore este e-mail: sua senha continua a mesma.</p>
             </div>
             """,
            $"Olá, {user.Name}.\n\nUse este código para criar uma nova senha no Zeno: {code}\n\n" +
            $"Ele vale por {minutes} minutos. Se não foi você que pediu, ignore este e-mail: sua senha continua a mesma.");
    }

    public async Task ResetAsync(ResetPasswordRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new AppValidationException(validation);

        var user = await _userRepository.GetByEmailAsync(request.Email.Trim());
        var stored = user is null ? null : await _codeRepository.GetByUserAsync(user.Id);
        if (user is null || stored is null || stored.ExpiresAt <= _clock.UtcNow || stored.Attempts >= MaxAttempts)
            throw InvalidCode();

        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(user.Id, request.Code)),
            Encoding.UTF8.GetBytes(stored.CodeHash));
        if (!matches)
        {
            await _codeRepository.IncrementAttemptsAsync(stored.Id);
            throw InvalidCode();
        }

        await _userRepository.UpdatePasswordAsync(user.Id, BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
        await _codeRepository.DeleteByUserAsync(user.Id);
        // Whoever knew the old password loses their sessions on the next refresh.
        await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);
    }

    /// <summary>Bound to the user so a stolen hash table can't be replayed across accounts.</summary>
    private static string Hash(Guid userId, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{code}")));

    private static AppValidationException InvalidCode() =>
        new(new FluentValidation.Results.ValidationResult(
            new List<FluentValidation.Results.ValidationFailure>
            {
                new("Code", "Código inválido ou expirado. Peça um novo código.")
            }));
}
