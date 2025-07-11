using Microsoft.Extensions.Configuration;
using Workers;
using Configuration;

// аргументы консоли
string repoName = "eos-storage";
string branchName = "issues/#127212_dev";
string slnPath = "";
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
                slnPath <path to .sln> - path to solution. If this is filled - tfs operation was skiped
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
        case "slnPath":
            {
                slnPath = vals[1];
                break;
            }
        default:
            return; 
    }
}

var slnPathIsFilled = !string.IsNullOrEmpty(slnPath);
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
        slnPath = tfsWorker.SlnPath;
    }
    Console.WriteLine("Анализ проекта");
    if (slnPathIsFilled)
    {
        repoName = Path.GetFileNameWithoutExtension(slnPath)!;
        branchName = repoName;
    }
    var dtWorker = new DTWorker(config.Dt, repoName, branchName);
    await dtWorker.ProcessAsync(slnPath, token);
    Console.WriteLine("Анализ проекта завершен");
}
finally
{
    Directory.Delete(tempDir, true);
    Console.WriteLine("Временная директория удалена");
}