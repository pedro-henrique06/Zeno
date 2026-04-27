using Zeno.Application.Requests;
using Zeno.Application.Responses;

namespace Zeno.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task LogoutAsync(string token);
}
