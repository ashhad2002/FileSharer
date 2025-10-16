using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using FileSharer.Services;
using FileSharer.Models;
using FileSharer.Tests.TestHelpers;

var builder = WebApplication.CreateBuilder(args);
string configFile = builder.Environment.IsEnvironment("Test") ? "appsettings.Test.json" : "appsettings.json";
string json = File.ReadAllText(configFile);
var config = JsonSerializer.Deserialize<Config>(json) ?? throw new InvalidOperationException("Failed to load configuration");

StorageClient? storageClient = null;
ServiceAccountCredential? serviceAccountCredential = null;

if (!builder.Environment.IsEnvironment("Test"))
{
    var credential = GoogleCredential.FromFile(config.keyFilename);
    storageClient = StorageClient.Create(credential);
    serviceAccountCredential = (ServiceAccountCredential)credential.UnderlyingCredential;
}

builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Configure Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(config.JWTKEY)),
        ValidateIssuer = false,
        ValidateAudience = false,
    };
});

builder.Services.AddAuthorization();

// Register services
builder.Services.AddSingleton(config);

if (!builder.Environment.IsEnvironment("Test"))
{
    builder.Services.AddSingleton(storageClient!);
    builder.Services.AddSingleton(serviceAccountCredential!);
    builder.Services.AddScoped<ICloudStorageService, CloudStorageService>();
}
else
{
    builder.Services.AddScoped<ICloudStorageService, MockCloudStorageService>();
}

builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Configure endpoints
app.MapGet("/files", async (IFileService fileService) => 
    await fileService.GetFilesAsync());

app.MapGet("/downloadurl", async (int fileId, IFileService fileService) =>
{
    var result = await fileService.GetDownloadUrlAsync(fileId);
    if (result.HasValue)
    {
        return Results.Ok(new { downloadUrl = result.Value.downloadUrl, fileName = result.Value.fileName });
    }
    return Results.NotFound();
});

app.MapPost("/upload", async (HttpRequest request, IFormFile file, IUserService userService, IFileService fileService) =>
{
    try
    {
        string? token = request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        int? uploaderId = null;

        if (!string.IsNullOrEmpty(token) && token != "null")
        {
            uploaderId = await userService.GetUserIdFromTokenAsync(token);
            if (uploaderId == null)
            {
                return Results.Unauthorized();
            }
        }

        bool success = await fileService.UploadFileAsync(file, uploaderId);
        return success ? Results.Ok() : Results.StatusCode(500);
    }
    catch
    {
        return Results.Unauthorized();
    }
});

app.MapPost("/register", async (User user, IUserService userService) => 
    await userService.RegisterAsync(user));

app.MapPost("/login", async (User loginUser, IUserService userService) =>
    await userService.LoginAsync(loginUser));

app.UseCors("AllowAllOrigins");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
