using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Simple_Real_Time_Chat.Authentication;

namespace Simple_Real_Time_Chat.Controllers;

[ApiController]
[Route("api/Auth")]
public sealed class AuthController : ControllerBase
{
    private readonly JwtOptions _jwtOptions;

    public AuthController(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequest(new { message = "Username is required." });
        }

        var normalizedUserName = request.UserName.Trim();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, normalizedUserName),
            new Claim(JwtRegisteredClaimNames.UniqueName, normalizedUserName),
            new Claim(ClaimTypes.Name, normalizedUserName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);

        var jwtToken = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        return Ok(new
        {
            token = accessToken,
            userName = normalizedUserName,
            expiresAtUtc = expires
        });
    }
}

public sealed record LoginRequest(string UserName);