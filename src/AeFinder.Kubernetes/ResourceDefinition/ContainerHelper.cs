namespace AeFinder.Kubernetes.ResourceDefinition;

public class ContainerHelper
{
    public static string GetAppContainerName(string appId, string version,string clientType)
    {
        return $"container-{appId}-{version}-{clientType}";
    }
}