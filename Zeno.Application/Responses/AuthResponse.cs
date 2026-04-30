namespace Zeno.Application.Responses;

public class AuthResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Document { get; set; }
    public DateTime? BirthDate { get; set; }
    public string OAuthProvider { get; set; } = "None";
    public string Token { get; set; } = string.Empty;
}