using FileSharer.Models;

namespace FileSharer.Services;

public interface IFileService
{
    Task<List<FileEntry>> GetFilesAsync();
    Task<(string downloadUrl, string fileName)?> GetDownloadUrlAsync(int fileId);
    Task<bool> UploadFileAsync(IFormFile file, int? uploaderId);
}
