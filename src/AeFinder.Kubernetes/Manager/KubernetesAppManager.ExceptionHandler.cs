using AElf.ExceptionHandler;
using k8s.Autorest;
using Microsoft.Extensions.Logging;

namespace AeFinder.Kubernetes.Manager;

public partial class KubernetesAppManager
{
    private FlowBehavior HandleHttpOperationException(HttpOperationException exception, string serviceMonitorName)
    {
        // Handle resources do not exist
        if (exception.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("The service monitor resource {ServiceMonitorName} does not exist.",
                serviceMonitorName);
            return new FlowBehavior
            {
                ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
                ReturnValue = false
            };
        }

        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        };
    }

    private FlowBehavior HandleException(Exception exception, string serviceMonitorName)
    {
        // Exceptions are caught here because normal business cannot fail due to the monitored service
        _logger.LogError(exception, "List service monitor resource {ServiceMonitorName} error.", serviceMonitorName);
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}