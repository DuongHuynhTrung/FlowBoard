using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using AuthService.Application.DTOs;
using AuthService.Application.Exceptions;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;

namespace AuthService.Tests
{
    public class AuthAppServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<IRoleRepository> _roleRepoMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<IPasswordService> _passwordServiceMock = new();
        private readonly Mock<IGoogleAuthService> _googleAuthMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        
        private readonly AuthAppService _authService;

        public AuthAppServiceTests()
        {
            _configMock.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("7");

            _authService = new AuthAppService(
                _userRepoMock.Object,
                _roleRepoMock.Object,
                _refreshTokenRepoMock.Object,
                _tokenServiceMock.Object,
                _passwordServiceMock.Object,
                _googleAuthMock.Object,
                _configMock.Object
            );
        }

        [Fact]
        public async Task Register_Success_ReturnsTokens()
        {
            // Arrange
            var request = new RegisterRequest { Email = "test@test.com", Password = "Pass123!", FullName = "Test User" };
            
            _userRepoMock.Setup(x => x.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
            _roleRepoMock.Setup(x => x.GetByNameAsync(RoleEnum.Member.ToString())).ReturnsAsync(new Role { Id = Guid.NewGuid(), Name = "Member" });
            _passwordServiceMock.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed_pwd");
            _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>())).Returns("access_token");
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");
            _tokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hashed_refresh_token");

            // Act
            var response = await _authService.RegisterAsync(request, "127.0.0.1");

            // Assert
            Assert.NotNull(response);
            Assert.Equal("access_token", response.AccessToken);
            Assert.Equal("refresh_token", response.RefreshToken);
            _userRepoMock.Verify(x => x.AddAsync(It.Is<User>(u => u.Email == request.Email)), Times.Once);
        }

        [Fact]
        public async Task Register_EmailAlreadyExists_ThrowsConflictException()
        {
            // Arrange
            var request = new RegisterRequest { Email = "test@test.com" };
            _userRepoMock.Setup(x => x.ExistsByEmailAsync(request.Email)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(request, "127.0.0.1"));
        }

        [Fact]
        public async Task Login_Success_ReturnsTokens()
        {
            // Arrange
            var request = new LoginRequest { Email = "test@test.com", Password = "Pass123!" };
            var user = new User { Id = Guid.NewGuid(), Email = request.Email, PasswordHash = "hash", IsActive = true };
            
            _userRepoMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(user);
            _passwordServiceMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash)).Returns(true);
            _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>())).Returns("access_token");
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh_token");

            // Act
            var response = await _authService.LoginAsync(request, "127.0.0.1");

            // Assert
            Assert.NotNull(response);
            Assert.Equal("access_token", response.AccessToken);
            _refreshTokenRepoMock.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
        }

        [Fact]
        public async Task Login_WrongPassword_ThrowsUnauthorizedException()
        {
            // Arrange
            var request = new LoginRequest { Email = "test@test.com", Password = "Wrong!" };
            var user = new User { Id = Guid.NewGuid(), PasswordHash = "hash" };
            
            _userRepoMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(user);
            _passwordServiceMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash)).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(request, "127.0.0.1"));
        }

        [Fact]
        public async Task Login_InactiveUser_ThrowsUnauthorizedException()
        {
             // Arrange
            var request = new LoginRequest { Email = "test@test.com", Password = "Pass" };
            var user = new User { Id = Guid.NewGuid(), PasswordHash = "hash", IsActive = false };
            
            _userRepoMock.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(user);
            _passwordServiceMock.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash)).Returns(true);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(request, "127.0.0.1"));
        }

        [Fact]
        public async Task RefreshToken_Success_RotatesTokens()
        {
            // Arrange
            var request = new RefreshRequest { RefreshToken = "old_refresh" };
            var userId = Guid.NewGuid();
            var storedToken = new RefreshToken { UserId = userId, TokenHash = "hash", ExpiresAt = DateTime.UtcNow.AddDays(1) };
            var user = new User { Id = userId, IsActive = true, UserRoles = new List<UserRole>() };

            _tokenServiceMock.Setup(x => x.HashToken(request.RefreshToken)).Returns("hash");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync("hash")).ReturnsAsync(storedToken);
            _userRepoMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>())).Returns("new_access");
            _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("new_refresh");
            _tokenServiceMock.Setup(x => x.HashToken("new_refresh")).Returns("new_hash");

            // Act
            var response = await _authService.RefreshTokenAsync(request, "192.168.1.1");

            // Assert
            Assert.NotNull(response);
            Assert.Equal("new_access", response.AccessToken);
            Assert.NotNull(storedToken.RevokedAt); // should reflect rotation
            _refreshTokenRepoMock.Verify(x => x.UpdateAsync(storedToken), Times.Once);
            _refreshTokenRepoMock.Verify(x => x.AddAsync(It.Is<RefreshToken>(r => r.TokenHash == "new_hash")), Times.Once);
        }

        [Fact]
        public async Task RefreshToken_Expired_ThrowsUnauthorizedException()
        {
            // Arrange
            var request = new RefreshRequest { RefreshToken = "expired_refresh" };
            var storedToken = new RefreshToken { TokenHash = "hash", ExpiresAt = DateTime.UtcNow.AddMinutes(-5) };
            
            _tokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hash");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync("hash")).ReturnsAsync(storedToken);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.RefreshTokenAsync(request, "127.0.0.1"));
        }

        [Fact]
        public async Task RefreshToken_AlreadyRevoked_ThrowsUnauthorizedException()
        {
            // Arrange
            var request = new RefreshRequest { RefreshToken = "revoked_refresh" };
            var storedToken = new RefreshToken { TokenHash = "hash", ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = DateTime.UtcNow.AddDays(-1) };
            
            _tokenServiceMock.Setup(x => x.HashToken(It.IsAny<string>())).Returns("hash");
            _refreshTokenRepoMock.Setup(x => x.GetByTokenHashAsync("hash")).ReturnsAsync(storedToken);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.RefreshTokenAsync(request, "127.0.0.1"));
        }
    }
}
