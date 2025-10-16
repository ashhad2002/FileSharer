using FileSharer.Models;
using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using Npgsql;

namespace FileSharer.Services;

public class FileService : IFileService
{
    private readonly string _connectionString;
    private readonly string _bucketName;
    private readonly StorageClient _storageClient;
    private readonly ServiceAccountCredential _credential;

    public FileService(Config config, StorageClient storageClient, ServiceAccountCredential credential)
    {
        _connectionString = config.connectionString;
        _bucketName = config.bucketName;
        _storageClient = storageClient;
        _credential = credential;
    }

    public async Task<List<FileEntry>> GetFilesAsync()
    {
        List<FileEntry> files = new List<FileEntry>();

        using (var connection = new NpgsqlConnection(_connectionString))
        {
            await connection.OpenAsync();
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
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
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
    }

    public async Task<(string downloadUrl, string fileName)?> GetDownloadUrlAsync(int fileId)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
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

                        var signedUrl = UrlSigner.FromCredential(_credential)
                            .Sign(_bucketName, storedFileName, TimeSpan.FromHours(1), HttpMethod.Get);

                        return (signedUrl, originalFileName);
                    }
                }
            }
        }

        return null;
    }

    public async Task<bool> UploadFileAsync(IFormFile file, int? uploaderId)
    {
        try
        {
            var originalFileName = file.FileName;
            var uniqueId = Guid.NewGuid().ToString();
            var extension = Path.GetExtension(file.FileName);
            var storedFileName = $"{Path.GetFileNameWithoutExtension(originalFileName)}_{uniqueId}{extension}";

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            await _storageClient.UploadObjectAsync(_bucketName, storedFileName, null, memoryStream);
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

            using (var connection = new NpgsqlConnection(_connectionString))
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

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsImageExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" => true,
            _ => false
        };
    }
}
