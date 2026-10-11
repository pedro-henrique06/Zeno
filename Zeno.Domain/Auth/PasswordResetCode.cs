namespace Zeno.Domain.Auth;

/// <summary>A one-time code emailed for "forgot password". Only its hash is stored.</summary>
public class PasswordResetCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; }
}
