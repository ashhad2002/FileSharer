using FileSharer.Models;

namespace FileSharer.Services;

public interface IUserService
{
    Task<IResult> RegisterAsync(User user);
    Task<IResult> LoginAsync(User loginUser);
    Task<int?> GetUserIdFromTokenAsync(string token);
}
