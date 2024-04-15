using k8s.Models;

namespace AeFinder.Kubernetes.ResourceDefinition;

public class NameSpaceHelper
{
    public static V1Namespace CreateNameSpaceDefinition(string nameSpace)
    {
        var newNamespace = new V1Namespace
        {
            Metadata = new V1ObjectMeta
            {
                Name = nameSpace 
            }
        };

        return newNamespace;
    }
}