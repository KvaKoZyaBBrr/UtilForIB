using Microsoft.Extensions.Configuration;

namespace Configuration;

public class DtConfiguration
{
    public DtConfiguration(IConfigurationSection configuration)
    {
        Token = configuration.GetSection("Token").Value!;
        var url = configuration.GetSection("Url").Value!;
        Url = new(url);
        SbomPath = configuration.GetSection("SbomPath").Value!;
    }
    
    public string Token { get; init; }
    public Uri Url { get; set; }
    public string SbomPath {get;set;}
}