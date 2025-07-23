using Microsoft.Extensions.Configuration;

namespace Configuration;

public class TfsConfiguration
{
    public TfsConfiguration(IConfigurationSection configuration)
    {
        Token = configuration.GetSection("Token").Value!;
        var url = configuration.GetSection("CollectionUrl").Value!;
        CollectionUrl = new(url);
        ProjectName = configuration.GetSection("ProjectName").Value!;
        ZipName = configuration.GetSection("ZipName").Value ?? "latest_commit.zip";
    }

    public string Token { get; init; }
    public Uri CollectionUrl { get; init; }
    public string ProjectName { get; init; }
    public string ZipName { get; init; } 
}