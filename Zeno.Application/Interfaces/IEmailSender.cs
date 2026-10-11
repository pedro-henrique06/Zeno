namespace Zeno.Application.Interfaces;

public interface IEmailSender
{
    /// <summary>Sends a transactional email. Returns false when it could not be sent.</summary>
    Task<bool> SendAsync(string to, string subject, string html, string text);
}
