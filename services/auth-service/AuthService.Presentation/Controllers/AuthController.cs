using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Interfaces;

namespace AuthService.Presentation.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthAppService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public AuthController(IAuthAppService authService, IUserRepository userRepository, IRoleRepository roleRepository)
        {
            _authService = authService;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.RegisterAsync(request, GetIpAddress());
            return Created(string.Empty, response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request, GetIpAddress());
            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request, GetIpAddress());
            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
        {
            await _authService.LogoutAsync(request.RefreshToken, GetIpAddress());
            return NoContent();
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var response = await _authService.GoogleLoginAsync(request, GetIpAddress());
            return Ok(response);
        }

        [Authorize]
        [HttpGet("users/me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            return Ok(new UserDto
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] string roleName)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound("User not found");

            var role = await _roleRepository.GetByNameAsync(roleName);
            if (role == null) return BadRequest("Role does not exist");

            user.UserRoles.Clear();
            user.UserRoles.Add(new Domain.Entities.UserRole { UserId = user.Id, RoleId = role.Id });
            await _userRepository.UpdateAsync(user);

            return Ok(new { Message = "Role updated successfully" });
        }
    }
}
