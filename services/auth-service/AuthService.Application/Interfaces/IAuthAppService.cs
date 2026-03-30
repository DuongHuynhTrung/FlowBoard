using System.Threading.Tasks;
using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces
{
    public interface IAuthAppService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress);
        Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
        Task<AuthResponse> RefreshTokenAsync(RefreshRequest request, string ipAddress);
        Task LogoutAsync(string refreshToken, string ipAddress);
        Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, string ipAddress);
    }
}
