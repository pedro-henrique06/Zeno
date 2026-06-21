namespace Zeno.Domain.User;

public enum OAuthProvider
{
    None = 0,
    Google = 1
}

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Document { get; set; }
    public DateTime? BirthDate { get; set; }
    public OAuthProvider Provider { get; set; } = OAuthProvider.None;
    public string? ProviderId { get; set; }
    public string? PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool EmailVerified { get; set; } = false;
    public decimal? DailyBudget { get; set; }
}