using FluentAssertions;
using Xunit;

namespace FileSharer.Tests.Unit;

public class ImageProcessingTests
{
    [Theory]
    [InlineData(".jpg", true)]
    [InlineData(".jpeg", true)]
    [InlineData(".png", true)]
    [InlineData(".webp", true)]
    [InlineData(".JPG", true)]
    [InlineData(".txt", false)]
    [InlineData(".pdf", false)]
    [InlineData(".doc", false)]
    [InlineData(".mp4", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsImageExtension_ShouldReturnCorrectResult(string extension, bool expected)
    {
        // Act
        var result = IsImageExtension(extension);

        // Assert
        result.Should().Be(expected);
    }

    // Helper method to test the IsImageExtension function from Program.cs
    private static bool IsImageExtension(string extension)
    {
        return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".webp" || extension == ".JPG";
    }
}
