using FluentAssertions;
using Xunit;

namespace FileSharer.Tests.Unit;

public class UserTests
{
    [Fact]
    public void User_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Username.Should().Be(string.Empty);
        user.Email.Should().Be(string.Empty);
        user.Password.Should().Be(string.Empty);
    }

    [Fact]
    public void User_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";
        var password = "testpassword123";

        // Act
        var user = new User
        {
            Username = username,
            Email = email,
            Password = password
        };

        // Assert
        user.Username.Should().Be(username);
        user.Email.Should().Be(email);
        user.Password.Should().Be(password);
    }
}
