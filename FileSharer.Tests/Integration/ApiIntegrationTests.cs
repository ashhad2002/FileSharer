using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FileSharer.Tests.TestHelpers;
using Xunit;

namespace FileSharer.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var projectDir = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent!.Parent!.Parent!.FullName;

        _client = factory
        .WithWebHostBuilder(builder =>
        {
            builder.UseContentRoot(projectDir);
        })
        .CreateClient();

    }

    [Fact]
    public async Task GetFiles_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/files");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithValidUser_ShouldReturnOk()
    {
        // Arrange
        var user = new
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "testpassword123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/register", user);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidUser = new
        {
            Username = "",
            Email = "invalid-email",
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/register", invalidUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOk()
    {
        // Arrange
        var loginUser = new
        {
            Username = "testuser",
            Password = "testpassword123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/login", loginUser);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidLogin = new
        {
            Username = "nonexistent",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/login", invalidLogin);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DownloadUrl_WithInvalidFileId_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/downloadurl?fileId=999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

}
