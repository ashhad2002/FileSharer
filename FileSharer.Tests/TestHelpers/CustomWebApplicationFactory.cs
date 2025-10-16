using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FileSharer.Services;

namespace FileSharer.Tests.TestHelpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseContentRoot(GetProjectPath());

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICloudStorageService));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            
            services.AddScoped<ICloudStorageService, MockCloudStorageService>();
        });
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