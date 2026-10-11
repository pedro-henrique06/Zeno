namespace Zeno.Application.Interfaces;

public interface IAccountDeletionService
{
    /// <summary>
    /// Permanently deletes the user and everything tied to them (App Store guideline 5.1.1(v)):
    /// financial data, integration keys, devices and notification settings, sessions, the houses
    /// they own (members lose access) and their membership in other houses, then the user record.
    /// </summary>
    Task DeleteAccountAsync(Guid userId);
}
