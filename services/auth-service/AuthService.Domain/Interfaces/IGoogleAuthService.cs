using System.Threading.Tasks;

namespace AuthService.Domain.Interfaces
{
    public class GoogleUserPayload
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Picture { get; set; }
    }

    public interface IGoogleAuthService
    {
        Task<GoogleUserPayload> VerifyGoogleTokenAsync(string idToken);
    }
}
