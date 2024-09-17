using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class AuthProduitController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthProduitController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] UserLogin userLogin)
    {
        // Vérification simplifiée des identifiants utilisateur
        if (userLogin.Username == "testuser" && userLogin.Password == "testpassword")
        {
            var token = GenerateJwtToken(userLogin.Username);
            return Ok(new { Token = token });
        }

        return Unauthorized(new { message = "Nom d'utilisateur ou mot de passe incorrect." });
    }

    private string GenerateJwtToken(string username)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, username)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(30), // Token valable pour 30 minutes
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Modèle pour les informations de connexion utilisateur
public class UserLogin
{
    public string Username { get; set; }
    public string Password { get; set; }
}
