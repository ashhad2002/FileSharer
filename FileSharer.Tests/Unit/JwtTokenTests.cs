using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using FluentAssertions;
using Xunit;

namespace FileSharer.Tests.Unit;

public class JwtTokenTests
{
    private readonly string _testJwtKey = "test-jwt-key-for-testing-purposes-only-must-be-long-enough";

    [Fact]
    public void CreateJwtToken_ShouldGenerateValidToken()
    {
        // Arrange
        var username = "testuser";
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_testJwtKey);

        // Act
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("username", username) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Assert
        tokenString.Should().NotBeNullOrEmpty();
        tokenHandler.CanReadToken(tokenString).Should().BeTrue();
    }

    [Fact]
    public void ReadJwtToken_ShouldExtractUsername()
    {
        // Arrange
        var username = "testuser";
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_testJwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("username", username) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        // Act
        var jwtToken = tokenHandler.ReadJwtToken(tokenString);
        var extractedUsername = jwtToken.Claims.FirstOrDefault(c => c.Type == "username")?.Value;

        // Assert
        extractedUsername.Should().Be(username);
    }

    [Fact]
    public void InvalidToken_ShouldNotBeReadable()
    {
        // Arrange
        var invalidToken = "invalid.tokenhere";
        var tokenHandler = new JwtSecurityTokenHandler();

        // Act
        var canRead = tokenHandler.CanReadToken(invalidToken);

        // Assert
        canRead.Should().BeFalse();
    }
}
