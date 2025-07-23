using Microsoft.Extensions.Configuration;
using Workers;
using Configuration;

// аргументы консоли
string repoName = "";
string branchName = "";
string[] slnPaths = [];
foreach (var arg in args)
{
    var vals = arg.Split("=");

    switch (vals[0])
    {
        case "help":
            {
                Console.WriteLine(@"How to use:
                cli args:
                help - this message
                repo <repository name> - reposytory name
                branch <branch name> - branch name
                slnPaths <paths to .sln>[,<<paths to .sln>>] - paths to solution. If this is filled - tfs operation was skiped
                another configs in appsettings.json");
                return;
            }
        case "repo":
            {
                repoName = vals[1];
                break;
            }
        case "branch":
            {
                branchName = vals[1];
                break;
            }
        case "slnPaths":
            {
                slnPaths = vals[1].Split(",");
                break;
            }
        default:
            return; 
    }
}

var slnPathIsFilled = slnPaths.Any();
var branchFilled = !string.IsNullOrEmpty(repoName) && !string.IsNullOrEmpty(branchName);

if (!slnPathIsFilled && !branchFilled)
    return;

var settings = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional:true)
    .Build();

var config = new MainConfiguration(settings);
Console.WriteLine("Конфиг прочитан");

CancellationTokenSource source = new CancellationTokenSource();
CancellationToken token = source.Token;

string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
Directory.CreateDirectory(tempDir);
Console.WriteLine("Создана временная директория");
try
{
    if (!slnPathIsFilled)
    {
        var tfsWorker = new TfsWorker(config.Tfs, repoName, branchName);
        await tfsWorker.ProcessAsync(tempDir, token);
        slnPaths = tfsWorker.SlnPaths;
    }
    Console.WriteLine("Анализ ...");
    foreach (var slnPath in slnPaths)
    {
        Console.WriteLine($"Анализ проекта {slnPath}");
        var projectName = Path.GetFileNameWithoutExtension(slnPath)!;
        if (string.IsNullOrEmpty(branchName))
            branchName = "manual";
        var dtWorker = new DTWorker(config.Dt, projectName, branchName);

        try
        {
            await dtWorker.ProcessAsync(slnPath, token);
            Console.WriteLine("Анализ проекта завершен");
        }
        catch (Exception ex)
        { 
            Console.WriteLine(ex.Message);
        }        
    }
    Console.WriteLine("Анализ завершен");
}
finally
{
    Directory.Delete(tempDir, true);
    Console.WriteLine("Временная директория удалена");
}