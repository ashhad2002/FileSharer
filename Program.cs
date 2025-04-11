using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Npgsql;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

string json = File.ReadAllText("appsettings.json");
var config = JsonSerializer.Deserialize<Config>(json);

var bucketName = config.bucketName;
var keyFilename = config.keyFilename;
var connectionString = config.connectionString;
var JWTKEY = config.JWTKEY;


var credential = GoogleCredential.FromFile(keyFilename);
var storageClient = StorageClient.Create(credential);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JWTKEY)),
        ValidateIssuer = false,
        ValidateAudience = false,
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.MapGet("/files", () =>
{
    List<FileEntry> files = new List<FileEntry>();

    using (var connection = new NpgsqlConnection(connectionString))
    {
        connection.Open();
        string query = @"
            SELECT 
                f.FileId,
                f.FileName,
                f.UploaderID,
                f.UploadDate,
                f.Thumbnail,
                u.Username AS UploaderName
            FROM Files f
            LEFT JOIN Users u ON f.UploaderID = u.UserId";

        using (var cmd = new NpgsqlCommand(query, connection))
        {
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var file = new FileEntry
                    {
                        FileId = reader.GetInt32(reader.GetOrdinal("FileId")),
                        FileName = reader.GetString(reader.GetOrdinal("FileName")),
                        UploaderID = reader.IsDBNull(reader.GetOrdinal("UploaderID")) ? -1 : reader.GetInt32(reader.GetOrdinal("UploaderID")),
                        UploadDate = reader.GetDateTime(reader.GetOrdinal("UploadDate")),
                        Thumbnail = reader.IsDBNull(reader.GetOrdinal("Thumbnail"))
                            ? null
                            : Convert.ToBase64String((byte[])reader["Thumbnail"]),
                        UploaderName = reader.IsDBNull(reader.GetOrdinal("UploaderName"))
                            ? "Anonymous"
                            : reader.GetString(reader.GetOrdinal("UploaderName"))
                    };

                    files.Add(file);
                }
            }
        }
    }

    return files;
});

app.MapGet("/downloadfile", async (int fileId) =>
{
    using (var connection = new NpgsqlConnection(connectionString))
    {
        await connection.OpenAsync();
        string query = "SELECT FileName, StoredFileName FROM Files WHERE FileId = @FileId";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@FileId", fileId);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    var originalFileName = reader.GetString(reader.GetOrdinal("FileName"));
                    var storedFileName = reader.GetString(reader.GetOrdinal("StoredFileName"));

                    using (var memoryStream = new MemoryStream())
                    {
                        storageClient.DownloadObject(bucketName, storedFileName, memoryStream);
                        return Results.File(
                            fileContents: memoryStream.ToArray(),
                            contentType: "application/octet-stream",
                            fileDownloadName: originalFileName
                        );
                    }
                }
            }
        }
    }

    return Results.NotFound();
});

app.MapPost("/upload", async (HttpRequest request, IFormFile file, IWebHostEnvironment env) =>
{
    int? uploaderId = null;

    try
    {
        string? token = request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        if (!string.IsNullOrEmpty(token) && token != "null") // Token can be casted to a literal null string, check for this
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
            {
                return Results.Unauthorized();
            }

            var jwtToken = handler.ReadJwtToken(token);

            var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "username")?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return Results.Unauthorized();
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string userQuery = "SELECT UserId FROM Users WHERE Username = @Username";
                using (var userCmd = new NpgsqlCommand(userQuery, connection))
                {
                    userCmd.Parameters.AddWithValue("@Username", username);
                    var result = await userCmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        uploaderId = Convert.ToInt32(result);
                    }
                    else
                    {
                        return Results.Unauthorized();
                    }
                }
            }
        }
    }
    catch
    {
        return Results.Unauthorized();
    }

    try
    {
        var originalFileName = file.FileName;
        var uniqueId = Guid.NewGuid().ToString();
        var extension = Path.GetExtension(file.FileName);
        var storedFileName = $"{Path.GetFileNameWithoutExtension(originalFileName)}_{uniqueId}{extension}";

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        await storageClient.UploadObjectAsync(bucketName, storedFileName, null, memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        byte[]? thumbnailBytes = null;
        if (IsImageExtension(extension))
        {
            try
            {
                using var image = await Image.LoadAsync(memoryStream);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(150, 150),
                    Mode = ResizeMode.Max
                }));

                using var thumbStream = new MemoryStream();
                await image.SaveAsync(thumbStream, new PngEncoder());
                thumbnailBytes = thumbStream.ToArray();
            }
            catch
            {
                thumbnailBytes = null;
            }
        }

        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            string query = "INSERT INTO Files (FileName, StoredFileName, Thumbnail, UploaderId) VALUES (@FileName, @StoredFileName, @Thumbnail, @UploaderId)";
            using (var cmd = new NpgsqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@FileName", originalFileName);
                cmd.Parameters.AddWithValue("@StoredFileName", storedFileName);
                cmd.Parameters.AddWithValue("@Thumbnail", thumbnailBytes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@UploaderId", uploaderId ?? (object)DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
    catch
    {
        return Results.StatusCode(500);
    }

    return Results.Ok();
});

bool IsImageExtension(string extension)
{
    return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".webp" || extension == ".JPG";
}

app.MapPost("/register", async (User user) => {   
    try
    {
        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            string searchQuery = "SELECT * FROM Users WHERE Username=@username OR Email=@email";
            using (NpgsqlCommand cmd = new NpgsqlCommand(searchQuery, connection))
            {
                cmd.Parameters.AddWithValue("@username", user.Username);
                cmd.Parameters.AddWithValue("@email", user.Email);

                using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return Results.BadRequest("Login Already in Use");
                    }
                }
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);
            string insertQuery = "INSERT INTO Users (username, email, password) VALUES (@username, @email, @password)";
            using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@username", user.Username);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@password", hashedPassword);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        return Results.Ok();
    }
    catch (Exception ex)
    {
        Console.WriteLine("", ex);
        return Results.Problem("An error occurred while processing your request.");
    }
});

app.MapPost("/login", async (User loginUser) =>
{
    try
    {
        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();

            string searchQuery = "SELECT Username, Password FROM Users WHERE Username=@username OR Email=@username";
            using (NpgsqlCommand cmd = new NpgsqlCommand(searchQuery, connection))
            {
                cmd.Parameters.AddWithValue("@username", loginUser.Username);

                using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        string storedUsername = reader.GetString(0);
                        string storedPassword = reader.GetString(1);

                        if (BCrypt.Net.BCrypt.Verify(loginUser.Password, storedPassword))
                        {
                            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                            var tokenDescriptor = new SecurityTokenDescriptor
                            {
                                Subject = new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("username", loginUser.Username) }),
                                Expires = DateTime.UtcNow.AddHours(1), 
                                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JWTKEY)), SecurityAlgorithms.HmacSha256Signature)
                            };
                            var token = tokenHandler.CreateToken(tokenDescriptor);
                            var tokenString = tokenHandler.WriteToken(token);

                            return Results.Ok(new { token = tokenString, userName=loginUser.Username });
                        }
                    }
                }
            }

            return Results.BadRequest("Invalid Username or Password");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex.Message}");
        return Results.Problem("An error occurred while processing your request.");
    }
});

app.UseCors("AllowAllOrigins");

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public class FileEntry
{
    public int FileId { get; set; }
    public string FileName { get; set; } = "default";
    public string StoredFileName { get; set; } = "";
    public int UploaderID { get; set; }
    public DateTime UploadDate { get; set; }
    public string? Thumbnail { get; set; }
    public string UploaderName { get; set; } = "Anonymous";
}

public class Config
{
    public string bucketName { get; set; } = "default";
    public string keyFilename { get; set; } = "default";

    public string connectionString { get; set; } = "Server=myServerAddress;Database=myDatabase;Uid=myUsername;Pwd=myPassword;";
    public string JWTKEY { get; set; } = "JWTKEY";
}

public class User
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
