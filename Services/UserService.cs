using FileSharer.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Npgsql;

namespace FileSharer.Services;

public class UserService : IUserService
{
    private readonly string _connectionString;
    private readonly string _jwtKey;

    public UserService(Config config)
    {
        _connectionString = config.connectionString;
        _jwtKey = config.JWTKEY;
    }

    public async Task<IResult> RegisterAsync(User user)
    {
        try
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
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
            Console.WriteLine($"Exception: {ex.Message}");
            return Results.Problem("An error occurred while processing your request.");
        }
    }

    public async Task<IResult> LoginAsync(User loginUser)
    {
        try
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
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
                                var token = GenerateJwtToken(loginUser.Username);
                                return Results.Ok(new { token = token, userName = loginUser.Username });
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
    }

    public async Task<int?> GetUserIdFromTokenAsync(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
            {
                return null;
            }

            var jwtToken = handler.ReadJwtToken(token);
            var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "username")?.Value;

            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string userQuery = "SELECT UserId FROM Users WHERE Username = @Username";
                using (var userCmd = new NpgsqlCommand(userQuery, connection))
                {
                    userCmd.Parameters.AddWithValue("@Username", username);
                    var result = await userCmd.ExecuteScalarAsync();
                    return result != null ? Convert.ToInt32(result) : null;
                }
            }
        }
        catch
        {
            return null;
        }
    }

    private string GenerateJwtToken(string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("username", username) }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
