using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using FileSharer.Services;
using FileSharer.Models;

string json = File.ReadAllText("appsettings.json");
var config = JsonSerializer.Deserialize<Config>(json);

var credential = GoogleCredential.FromFile(config.keyFilename);
var storageClient = StorageClient.Create(credential);
var serviceAccountCredential = (ServiceAccountCredential)credential.UnderlyingCredential;

var builder = WebApplication.CreateBuilder(args);
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
builder.Services.AddSingleton(storageClient);
builder.Services.AddSingleton(serviceAccountCredential);
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICloudStorageService, CloudStorageService>();

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
