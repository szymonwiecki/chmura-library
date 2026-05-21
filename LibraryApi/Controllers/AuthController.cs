using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibraryApi.Models;

namespace LibraryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _key;

        public AuthController(IConfiguration config)
        {
            _key = config.GetValue<string>("JwtKey") ?? "YourSecretKey12345"; // Użyj klucza z konfiguracji
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Authorization", Description = "This endpoint handles user authorization.")]
        public ActionResult Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest.Username != "admin" || loginRequest.Password != "admin")
                return Unauthorized(new { message = "Invalid credentials" });

            var token = GenerateJwtToken(loginRequest.Username);
            return Ok(new { token });
        }

        private string GenerateJwtToken(string username)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
