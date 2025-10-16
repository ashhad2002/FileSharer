namespace FileSharer.Models;

public class FileEntry
{
    public int FileId { get; set; }
    public string FileName { get; set; } = "default";
    public string StoredFileName { get; set; } = "";
    public int UploaderID { get; set; }
    public DateTime UploadDate { get; set; }
    public string? Thumbnail { get; set; }
    public string UploaderName { get; set; } = "Anonymous";
}
