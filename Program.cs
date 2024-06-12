using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using Npgsql;
using BCrypt.Net;

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

    using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
    {
        connection.Open();
        string query = "SELECT * FROM Files";
        using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
        {
            using (NpgsqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    FileEntry file = new FileEntry
                    {
                        FileId = reader.GetInt32(reader.GetOrdinal("FileId")),
                        FileName = reader.GetString(reader.GetOrdinal("FileName")),
                        UploaderID = reader.IsDBNull(reader.GetOrdinal("UploaderID")) ? -1 : reader.GetInt32(reader.GetOrdinal("UploaderID")),
                        UploadDate = reader.GetDateTime(reader.GetOrdinal("UploadDate"))
                    };
                    files.Add(file);
                }
            }
        }
    }

    return files;
});

app.MapGet("/downloadfile", (string fileName) =>
{
    using (var memoryStream = new MemoryStream())
    {
        storageClient.DownloadObject(bucketName, fileName, memoryStream);
        return new FileContentResult(memoryStream.ToArray(), "application/octet-stream")
        {
            FileDownloadName = fileName
        };
    }
});

app.MapPost("/upload", async (IFormFile file, IWebHostEnvironment env) =>
{
    try {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        
        await storageClient.UploadObjectAsync(bucketName, file.FileName, null, memoryStream);
        Console.WriteLine($"Uploaded {file.FileName} to {bucketName}.");

        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            string query = $"INSERT INTO Files (FileName) VALUES (@FileName)";
            using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@FileName", file.FileName);
                cmd.ExecuteNonQuery();
            }
        }
    }
    catch(Exception ex) {
        Console.WriteLine("", ex);
    }
    return Results.Ok(new { fileName = file.FileName });

    //code to upload locally, this is probably the easier way as compared to GCS, but its not as cool
    // var uploads = Path.Combine(env.ContentRootPath, "uploads");

    // if (!Directory.Exists(uploads))
    // {
    //     Directory.CreateDirectory(uploads);
    // }

    // Console.WriteLine(file.FileName);
    // var filePath = Path.Combine(uploads, file.FileName);

    // using (var stream = new FileStream(filePath, FileMode.Create))
    // {
    //     await file.CopyToAsync(stream);
    // }
});

app.MapPost("/register", async (User user) => {   
    try
    {
        Console.WriteLine("user:" + user.Username);
        Console.WriteLine("user:" + user.Password);
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

                            return Results.Ok(new { token = tokenString });
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
    public int UploaderID { get; set; }
    public DateTime UploadDate { get; set; }
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
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}