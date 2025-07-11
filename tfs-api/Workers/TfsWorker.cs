using System.IO.Compression;
using Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Workers;

public class TfsWorker(
        TfsConfiguration configuration,
        string repoName,
        string branchName
    ) : IWorker
{
    string _targetPath;
    public string SlnPath { get; private set; } = null;
    public async Task<bool> ProcessAsync(string data, CancellationToken cancellation)
    {
        _targetPath = data;

        if (await LoadRepositoryToTargetPath(cancellation))
        {
            SlnPath = FindSolutionOrProjectFile();
            return true;
        }

        return false;
    }

    private async Task<bool> LoadRepositoryToTargetPath(CancellationToken cancellation)
    {
        Console.WriteLine("Получение соединения");
        var orgUrl = configuration.CollectionUrl;
        var connection = new VssConnection(orgUrl, new VssBasicCredential(string.Empty, configuration.Token));
        using var gitClient = connection.GetClient<GitHttpClient>();

        await DownloadLastCommitFiles(gitClient, cancellation);
        return true;
    }

    private async Task DownloadLastCommitFiles(GitHttpClient gitClient, CancellationToken cancellation)
    {
        Console.WriteLine("Получение информации о проекте");
        var repo = gitClient.GetRepositoryAsync(configuration.ProjectName, repoName, cancellationToken:cancellation).Result;

        var branch = await gitClient.GetBranchAsync(repo.Id, branchName, cancellationToken: cancellation);
        var commit = branch.Commit;

        Console.WriteLine("Скачивание проекта...");
        // Download zip and extract to targetPath
        using var zipStream = await gitClient.GetItemZipAsync(repo.Id, "/", versionDescriptor: new GitVersionDescriptor()
        {
            Version = commit.CommitId,
            VersionType = GitVersionType.Commit
        }, cancellationToken: cancellation);

        using (var fileStream = File.Create(configuration.ZipName))
        {
            zipStream.CopyTo(fileStream);
        }

        Console.WriteLine("Распаковка проекта...");
        ZipFile.ExtractToDirectory(configuration.ZipName, _targetPath);
        File.Delete(configuration.ZipName);
        Console.WriteLine("Проект загружен");
    }

    private string FindSolutionOrProjectFile()
    {
        // Ищем файл .sln
        var slnFiles = Directory.GetFiles(_targetPath, "*.sln", SearchOption.AllDirectories);
        if (slnFiles.Length > 0)
            return slnFiles[0];

        // Если .sln нет, ищем .csproj
        var csprojFiles = Directory.GetFiles(_targetPath, "*.csproj", SearchOption.AllDirectories);
        if (csprojFiles.Length > 0)
            return csprojFiles[0];

        return null;
    }
}