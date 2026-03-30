using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;
using AuthService.Domain.Interfaces;

namespace AuthService.Infrastructure.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _configuration;

        public GoogleAuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<GoogleUserPayload> VerifyGoogleTokenAsync(string idToken)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new[] { _configuration["Google:ClientId"] }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            if (payload == null) return null;

            return new GoogleUserPayload
            {
                Email = payload.Email,
                Name = payload.Name,
                Picture = payload.Picture
            };
        }
    }
}
