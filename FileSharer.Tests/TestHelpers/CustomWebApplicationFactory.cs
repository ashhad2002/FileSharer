using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace FileSharer.Tests.TestHelpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(GetProjectPath());
    }

    private static string GetProjectPath()
    {
        string projectDir = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(projectDir, "Program.cs")))
        {
            projectDir = Directory.GetParent(projectDir)?.FullName 
                ?? throw new DirectoryNotFoundException("Could not find project root directory");
        }
        return projectDir;
    }
}