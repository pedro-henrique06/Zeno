using Zeno.Domain.Auth;

namespace Zeno.Domain.Interfaces;

public interface IPasswordResetCodeRepository
{
    /// <summary>The user's current code, if any (a new request replaces the previous one).</summary>
    Task<PasswordResetCode?> GetByUserAsync(Guid userId);

    /// <summary>Stores the code, replacing any earlier code for the same user.</summary>
    Task ReplaceAsync(PasswordResetCode code);

    Task IncrementAttemptsAsync(Guid id);
    Task DeleteByUserAsync(Guid userId);
}
