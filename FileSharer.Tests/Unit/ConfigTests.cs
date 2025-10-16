using FluentAssertions;
using Xunit;
using FileSharer.Models;

namespace FileSharer.Tests.Unit;

public class ConfigTests
{
    [Fact]
    public void Config_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var config = new Config();

        // Assert
        config.bucketName.Should().Be("default");
        config.keyFilename.Should().Be("default");
        config.connectionString.Should().Be("Server=myServerAddress;Database=myDatabase;Uid=myUsername;Pwd=myPassword;");
        config.JWTKEY.Should().Be("JWTKEY");
    }

    [Fact]
    public void Config_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var bucketName = "test-bucket";
        var keyFilename = "test-key.json";
        var connectionString = "Host=localhost;Database=testdb;Username=test;Password=test";
        var jwtKey = "test-jwt-key";

        // Act
        var config = new Config
        {
            bucketName = bucketName,
            keyFilename = keyFilename,
            connectionString = connectionString,
            JWTKEY = jwtKey
        };

        // Assert
        config.bucketName.Should().Be(bucketName);
        config.keyFilename.Should().Be(keyFilename);
        config.connectionString.Should().Be(connectionString);
        config.JWTKEY.Should().Be(jwtKey);
    }
}
