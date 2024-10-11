using System;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace AeFinder.DevelopmentTemplate;

public partial class DevelopmentTemplateAppService
{
    private FlowBehavior HandleGenerateProjectFileException(Exception exception, string projectName, string zipFileName,
        string generatedPath)
    {
        // Log the exception information and throw a UserFriendlyException to the user.
        var message = $"Generate project: {projectName} failed.";
        Logger.LogError(exception, message);

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(message)
        };
    }
    
    private FlowBehavior HandleCleanTempFilesException(Exception exception, string projectName, string zipFileName,
        string generatedPath)
    {
        // Only exception information is logged without blocking the service logic.
        Logger.LogError(exception, "Failed to clean up temporary files: {PorjectName}, {Path}.", projectName,
            generatedPath);

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        };
    }
}