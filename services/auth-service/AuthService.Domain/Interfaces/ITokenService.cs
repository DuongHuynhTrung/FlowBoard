using System.Collections.Generic;
using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user, IList<string> roles);
        string GenerateRefreshToken();
        string HashToken(string token);
        bool ValidateRefreshToken(string token, RefreshToken storedToken);
    }
}
