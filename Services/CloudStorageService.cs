using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using FileSharer.Models;

namespace FileSharer.Services;

public class CloudStorageService : ICloudStorageService
{
    private readonly string _bucketName;
    private readonly StorageClient _storageClient;
    private readonly ServiceAccountCredential _credential;

    public CloudStorageService(Config config, StorageClient storageClient, ServiceAccountCredential credential)
    {
        _bucketName = config.bucketName;
        _storageClient = storageClient;
        _credential = credential;
    }

    public async Task<string> GenerateDownloadUrlAsync(string fileName)
    {
        return UrlSigner.FromCredential(_credential)
            .Sign(_bucketName, fileName, TimeSpan.FromHours(1), HttpMethod.Get);
    }

    public async Task UploadFileAsync(string fileName, Stream fileStream)
    {
        await _storageClient.UploadObjectAsync(_bucketName, fileName, null, fileStream);
    }
}
