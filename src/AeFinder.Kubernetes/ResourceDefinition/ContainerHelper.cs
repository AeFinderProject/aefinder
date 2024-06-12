namespace AeFinder.Kubernetes.ResourceDefinition;

public class ContainerHelper
{
    public static string GetAppContainerName(string appId, string version,string clientType)
    {
        appId = appId.Replace("_", "-");
        return $"container-{version}-{clientType}".ToLower();
    }
}