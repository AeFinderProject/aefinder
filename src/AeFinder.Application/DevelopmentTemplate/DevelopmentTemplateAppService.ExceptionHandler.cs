using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace AeFinder.DevelopmentTemplate;

public partial class DevelopmentTemplateAppService
{
    private Task<FlowBehavior> HandleGenerateProjectFileExceptionAsync(Exception exception, string projectName, string zipFileName,
        string generatedPath)
    {
        // Log the exception information and throw a UserFriendlyException to the user.
        var message = $"Generate project: {projectName} failed.";
        Logger.LogError(exception, message);

        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new UserFriendlyException(message)
        });
    }
    
    private Task<FlowBehavior> HandleCleanTempFilesExceptionAsync(Exception exception, string zipFileName, string generatedPath)
    {
        // Only exception information is logged without blocking the service logic.
        Logger.LogError(exception, "Failed to clean up temporary files: {ZipFileName}, {Path}.", zipFileName,
            generatedPath);

        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        });
    }
}