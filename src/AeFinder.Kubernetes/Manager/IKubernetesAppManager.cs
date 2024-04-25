namespace AeFinder.Kubernetes.Manager;

public interface IKubernetesAppManager
{
    Task<string> CreateNewAppPodAsync(string appId, string version, string imageName);
    Task DestroyAppPodAsync(string appId, string version);
}