using FluentAssertions;
using Xunit;
using FileSharer.Models;

namespace FileSharer.Tests.Unit;

public class FileEntryTests
{
    [Fact]
    public void FileEntry_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var fileEntry = new FileEntry();

        // Assert
        fileEntry.FileId.Should().Be(0);
        fileEntry.FileName.Should().Be("default");
        fileEntry.StoredFileName.Should().Be("");
        fileEntry.UploaderID.Should().Be(0);
        fileEntry.UploadDate.Should().Be(default(DateTime));
        fileEntry.Thumbnail.Should().BeNull();
        fileEntry.UploaderName.Should().Be("Anonymous");
    }

    [Fact]
    public void FileEntry_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var fileId = 1;
        var fileName = "test-file.jpg";
        var storedFileName = "test-file_123.jpg";
        var uploaderId = 1;
        var uploadDate = DateTime.UtcNow;
        var thumbnail = "base64-thumbnail";
        var uploaderName = "testuser";

        // Act
        var fileEntry = new FileEntry
        {
            FileId = fileId,
            FileName = fileName,
            StoredFileName = storedFileName,
            UploaderID = uploaderId,
            UploadDate = uploadDate,
            Thumbnail = thumbnail,
            UploaderName = uploaderName
        };

        // Assert
        fileEntry.FileId.Should().Be(fileId);
        fileEntry.FileName.Should().Be(fileName);
        fileEntry.StoredFileName.Should().Be(storedFileName);
        fileEntry.UploaderID.Should().Be(uploaderId);
        fileEntry.UploadDate.Should().Be(uploadDate);
        fileEntry.Thumbnail.Should().Be(thumbnail);
        fileEntry.UploaderName.Should().Be(uploaderName);
    }
}
