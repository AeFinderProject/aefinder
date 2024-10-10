using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.DevelopmentTemplate;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class DevelopmentTemplateAppService : AeFinderAppService, IDevelopmentTemplateAppService
{
    private readonly DevTemplateOptions _devTemplateOptions;

    private const string TemplateProjectFolder = "template";
    private const string GeneratedProjectFolder = "generated";

    public DevelopmentTemplateAppService(IOptionsSnapshot<DevTemplateOptions> devTemplateOptions)
    {
        _devTemplateOptions = devTemplateOptions.Value;
    }

    public async Task<FileContentResult> GenerateProjectAsync(GenerateProjectDto input)
    {
        var tempFolder = Guid.NewGuid().ToString("N");
        var generatedPath = Path.Combine(_devTemplateOptions.TemplatePath, GeneratedProjectFolder, tempFolder);
        var zipFileName = generatedPath + ".zip";

        var file = await GenerateProjectFileAsync(input.Name, zipFileName, generatedPath);
        return new FileContentResult(file, "application/zip");
    }

    private async Task<byte[]> GenerateProjectFileAsync(string projectName, string zipFileName, string generatedPath)
    {
        try
        {
            GenerateProject(projectName, _devTemplateOptions.TemplatePath, generatedPath);
            ZipHelper.ZipDirectory(zipFileName, generatedPath);
            return await File.ReadAllBytesAsync(zipFileName);
        }
        catch (Exception e)
        {
            // Log the exception information and throw a UserFriendlyException to the user.
            var message = $"Generate project: {projectName} failed.";
            Logger.LogError(e, message);
            throw new UserFriendlyException(message);
        }
        finally
        {
            await CleanTempFilesAsync(zipFileName, generatedPath);
        }
    }

    private Task CleanTempFilesAsync(string zipFileName, string generatedPath)
    {
        try
        {
            File.Delete(zipFileName);
            Directory.Delete(generatedPath, true);
        }
        catch (Exception e)
        {
            // Only exception information is logged without blocking the service logic.
            Logger.LogError(e, "Failed to clean up temporary files.");
        }

        return Task.CompletedTask;
    }

    public void GenerateProject(string projectName, string templatePath, string generatedPath)
    {
        var projectFileReplacements = GetProjectFileReplacements(projectName);
        var generalReplacements = GetGeneralReplacements(projectName);

        var generatedFiles = new Queue<string>();
        var originDir = new DirectoryInfo(Path.Combine(templatePath, TemplateProjectFolder));
        var destDir = CreateDir(generatedPath);

        var queue = new Queue<DirectoryInfo>();
        queue.Enqueue(originDir);
        do
        {
            var dir = queue.Dequeue();
            foreach (var directoryInfo in dir.GetDirectories())
            {
                queue.Enqueue(directoryInfo);

                var destSubDir = destDir.FullName + directoryInfo.FullName.Replace(originDir.FullName, "");
                CreateDir(Replace(destSubDir, projectFileReplacements));
            }

            var files = dir.GetFiles();
            foreach (var originFile in files)
            {
                var destFileName = originFile.FullName.Replace(originDir.FullName, "");
                destFileName = Replace(destFileName, projectFileReplacements);

                destFileName = destDir.FullName + destFileName;
                originFile.CopyTo(destFileName, true);

                generatedFiles.Enqueue(destFileName);
            }
        } while (queue.Count > 0);

        while (generatedFiles.TryDequeue(out var file))
        {
            var extension = Path.GetExtension(file);
            if (extension == null || !_devTemplateOptions.ReplaceExtensions.Contains(extension))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            if (_devTemplateOptions.ProjectExtensions.Contains(extension))
            {
                content = Replace(content, projectFileReplacements);
            }
            else
            {
                content = Replace(content, generalReplacements);
            }

            File.WriteAllText(file, content);
        }
    }

    private static DirectoryInfo CreateDir(string path)
    {
        return Directory.Exists(path) ? new DirectoryInfo(path) : Directory.CreateDirectory(path);
    }
    
    private string Replace(string input, List<Tuple<string, string>> replacements)
    {
        return replacements.Aggregate(input,
            (current, replacement) => current.Replace(replacement.Item1, replacement.Item2));
    }

    private List<Tuple<string, string>> GetProjectFileReplacements(string projectName)
    {
        var replacement = new List<Tuple<string, string>> { new(_devTemplateOptions.ProjectPlaceholder, projectName) };

        return replacement;
    }

    private List<Tuple<string, string>> GetGeneralReplacements(string projectName)
    {
        var replacement = new List<Tuple<string, string>>
        {
            new ($"using {_devTemplateOptions.ProjectPlaceholder}", $"using {projectName}"),
            new ($"namespace {_devTemplateOptions.ProjectPlaceholder}", $"namespace {projectName}"),
            new (_devTemplateOptions.ProjectPlaceholder, projectName.Replace(".",""))
        };

        return replacement;
    }
}