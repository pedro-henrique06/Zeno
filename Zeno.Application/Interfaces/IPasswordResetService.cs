using Zeno.Application.Requests;

namespace Zeno.Application.Interfaces;

public interface IPasswordResetService
{
    /// <summary>
    /// Emails a 6-digit code if the address belongs to an email+password account. Says nothing either
    /// way, so the endpoint can't be used to find out who has an account.
    /// </summary>
    Task RequestAsync(ForgotPasswordRequest request);

    /// <summary>Checks the code and sets the new password, ending every other session.</summary>
    Task ResetAsync(ResetPasswordRequest request);
}
