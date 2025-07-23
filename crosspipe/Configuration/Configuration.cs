using Microsoft.Extensions.Configuration;

namespace Configuration;

public class MainConfiguration
{
    public MainConfiguration(IConfigurationRoot configuration)
    {
        Tfs = new(configuration.GetSection("Tfs"));
        Dt = new(configuration.GetSection("Dt"));
    }

    public DtConfiguration Dt { get; init; }
    public TfsConfiguration Tfs { get; init; }
}