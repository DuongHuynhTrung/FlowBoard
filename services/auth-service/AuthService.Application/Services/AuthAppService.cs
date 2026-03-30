using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Exceptions;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services
{
    public class AuthAppService : IAuthAppService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly IPasswordService _passwordService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IConfiguration _configuration;

        public AuthAppService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            IPasswordService passwordService,
            IGoogleAuthService googleAuthService,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _passwordService = passwordService;
            _googleAuthService = googleAuthService;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress)
        {
            if (await _userRepository.ExistsByEmailAsync(request.Email))
            {
                throw new ConflictException("Email already in use.");
            }

            var memberRole = await _roleRepository.GetByNameAsync(RoleEnum.Member.ToString());
            if (memberRole == null)
            {
                throw new Exception("Default role not found.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = _passwordService.HashPassword(request.Password),
                FullName = request.FullName,
                UserRoles = new System.Collections.Generic.List<UserRole>()
            };

            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = memberRole.Id, Role = memberRole });

            await _userRepository.AddAsync(user);

            return await GenerateAuthResponseAsync(user, ipAddress);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedException("Invalid credentials.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedException("Account is disabled.");
            }

            return await GenerateAuthResponseAsync(user, ipAddress);
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshRequest request, string ipAddress)
        {
            var tokenHash = _tokenService.HashToken(request.RefreshToken);
            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (storedToken == null)
            {
                throw new UnauthorizedException("Invalid refresh token.");
            }

            if (storedToken.RevokedAt != null)
            {
                // Suspected reuse attack
                throw new UnauthorizedException("Token has been revoked.");
            }

            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedException("Token has expired.");
            }

            var user = await _userRepository.GetByIdAsync(storedToken.UserId);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedException("Account is inactive or not found.");
            }

            // Rotate token
            var newRefreshTokenString = _tokenService.GenerateRefreshToken();
            var newRefreshTokenHash = _tokenService.HashToken(newRefreshTokenString);
            
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = ipAddress;
            storedToken.ReplacedByToken = newRefreshTokenHash;
            await _refreshTokenRepository.UpdateAsync(storedToken);

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var newAccessToken = _tokenService.GenerateAccessToken(user, roles);

            var days = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = newRefreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(days),
                CreatedByIp = ipAddress
            };
            await _refreshTokenRepository.AddAsync(newRefreshToken);

            return new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshTokenString,
                User = new UserDto
                {
                    Id = user.Id.ToString(),
                    Email = user.Email,
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarUrl
                }
            };
        }

        public async Task LogoutAsync(string refreshToken, string ipAddress)
        {
            var tokenHash = _tokenService.HashToken(refreshToken);
            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (storedToken != null && storedToken.RevokedAt == null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                storedToken.RevokedByIp = ipAddress;
                await _refreshTokenRepository.UpdateAsync(storedToken);
            }
        }

        public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, string ipAddress)
        {
            var payload = await _googleAuthService.VerifyGoogleTokenAsync(request.IdToken);
            if (payload == null)
            {
                throw new UnauthorizedException("Invalid Google token.");
            }

            var user = await _userRepository.GetByEmailAsync(payload.Email);
            if (user == null)
            {
                var memberRole = await _roleRepository.GetByNameAsync(RoleEnum.Member.ToString());
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = payload.Email,
                    // Use a dummy random password for oauth users
                    PasswordHash = _passwordService.HashPassword(Guid.NewGuid().ToString()),
                    FullName = payload.Name,
                    AvatarUrl = payload.Picture,
                    UserRoles = new System.Collections.Generic.List<UserRole>()
                };
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = memberRole.Id, Role = memberRole });
                await _userRepository.AddAsync(user);
            }
            else if (!user.IsActive)
            {
                throw new UnauthorizedException("Account is disabled.");
            }

            return await GenerateAuthResponseAsync(user, ipAddress);
        }

        private async Task<AuthResponse> GenerateAuthResponseAsync(User user, string ipAddress)
        {
            var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new System.Collections.Generic.List<string>();
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            
            var refreshTokenString = _tokenService.GenerateRefreshToken();
            var days = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "7");
            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = _tokenService.HashToken(refreshTokenString),
                ExpiresAt = DateTime.UtcNow.AddDays(days),
                CreatedByIp = ipAddress
            };
            await _refreshTokenRepository.AddAsync(newRefreshToken);

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                User = new UserDto
                {
                    Id = user.Id.ToString(),
                    Email = user.Email,
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarUrl
                }
            };
        }
    }
}
