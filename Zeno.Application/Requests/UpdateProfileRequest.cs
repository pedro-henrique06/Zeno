namespace Zeno.Application.Requests;

public class UpdateProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Document { get; set; }
    public DateTime? BirthDate { get; set; }
}
