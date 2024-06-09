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

        try
        {
            GenerateProject(input.Name, _devTemplateOptions.TemplatePath, generatedPath);
            ZipHelper.ZipDirectory(zipFileName, generatedPath);
            var file = await File.ReadAllBytesAsync(zipFileName);
            return new FileContentResult(file, "application/zip");
        }
        catch (Exception e)
        {
            var message = $"Generate project: {input.Name} failed.";
            Logger.LogError(e, message);
            throw new UserFriendlyException(message);
        }
        finally
        {
            try
            {
                File.Delete(zipFileName);
                Directory.Delete(generatedPath, true);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to clean up temporary files.");
            }
        }
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