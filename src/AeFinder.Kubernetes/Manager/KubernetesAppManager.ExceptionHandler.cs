using AElf.ExceptionHandler;
using k8s.Autorest;
using Microsoft.Extensions.Logging;

namespace AeFinder.Kubernetes.Manager;

public partial class KubernetesAppManager
{
    private Task<FlowBehavior> HandleHttpOperationExceptionAsync(HttpOperationException exception, string serviceMonitorName)
    {
        // Handle resources do not exist
        if (exception.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("The service monitor resource {ServiceMonitorName} does not exist.",
                serviceMonitorName);
            return Task.FromResult(new FlowBehavior
            {
                ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
                ReturnValue = false
            });
        }

        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
        });
    }

    private Task<FlowBehavior> HandleExceptionAsync(Exception exception, string serviceMonitorName)
    {
        // Exceptions are caught here because normal business cannot fail due to the monitored service
        _logger.LogError(exception, "List service monitor resource {ServiceMonitorName} error.", serviceMonitorName);
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        });
    }
}