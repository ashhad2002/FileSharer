using Google.Cloud.Storage.V1;

namespace FileSharer.Services;

public interface ICloudStorageService
{
    Task<string> GenerateDownloadUrlAsync(string fileName);
    Task UploadFileAsync(string fileName, Stream fileStream);
}
