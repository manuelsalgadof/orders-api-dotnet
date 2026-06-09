using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OrdersApi.DTOs;
using OrdersApi.Entities;
using OrdersApi.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OrdersApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public AuthController(IConfiguration configuration, IUserService userService)
        {
            _configuration = configuration;
            _userService   = userService;
        }

        [HttpPost("Generartoken")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var user = await _userService.ValidateCredentialsAsync(request.Email, request.Password);

            if (user is null)
                return Unauthorized(new { message = "Credenciales inválidas." });

            var token = GenerateJwtToken(user);

            return Ok(new LoginResponseDto { Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey   = _configuration["Jwt:Key"]!;
            var issuer   = _configuration["Jwt:Issuer"]!;
            var audience = _configuration["Jwt:Audience"]!;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
