using FileSharer.Services;

namespace FileSharer.Tests.TestHelpers;

public class MockCloudStorageService : ICloudStorageService
{
    public Task<string> GenerateDownloadUrlAsync(string fileName)
    {
        return Task.FromResult($"https://mock-storage.test/{fileName}");
    }

    public Task UploadFileAsync(string fileName, Stream fileStream)
    {
        return Task.CompletedTask;
    }
}
