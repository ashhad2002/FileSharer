namespace FileSharer.Models;

public class Config
{
    public string bucketName { get; set; } = "default";
    public string keyFilename { get; set; } = "default";
    public string connectionString { get; set; } = "Server=myServerAddress;Database=myDatabase;Uid=myUsername;Pwd=myPassword;";
    public string JWTKEY { get; set; } = "JWTKEY";
}
