using k8s.Models;

namespace AeFinder.Kubernetes.ResourceDefinition;

public class DeploymentHelper
{
    public static string GetAppDeploymentName(string appId, string version, string clientType)
    {
        appId = appId.Replace("_", "-");
        return $"deployment-{appId}-{version}-{clientType}".ToLower();
    }
    
    /// <summary>
    /// label name must be no more than 63 characters
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="version"></param>
    /// <param name="clientType"></param>
    /// <returns></returns>
    public static string GetAppDeploymentLabelName(string version, string clientType)
    {
        return $"deployment-{version}-{clientType}".ToLower();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="imageName"></param>
    /// <param name="deploymentName"></param>
    /// <param name="deploymentLabelName">must be no more than 63 characters</param>
    /// <param name="replicasCount"></param>
    /// <param name="containerName"></param>
    /// <param name="containerPort"></param>
    /// <param name="configMapName"></param>
    /// <param name="sideCarConfigMapName"></param>
    /// <returns></returns>
    public static V1Deployment CreateAppDeploymentWithFileBeatSideCarDefinition(string imageName, string deploymentName,
        string deploymentLabelName, int replicasCount, string containerName, int containerPort, string configMapName, string sideCarConfigMapName)
    {
        var labels = new Dictionary<string, string>
        {
            { KubernetesConstants.AppLabelKey, deploymentLabelName }
        };

        var deployment = new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = deploymentName,
                NamespaceProperty = KubernetesConstants.AppNameSpace
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = replicasCount,
                Selector = new V1LabelSelector { MatchLabels = labels },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta { Labels = labels },
                    Spec = new V1PodSpec
                    {
                        Affinity = new V1Affinity
                        {
                            NodeAffinity = new V1NodeAffinity
                            {
                                RequiredDuringSchedulingIgnoredDuringExecution = new V1NodeSelector
                                {
                                    NodeSelectorTerms = new List<V1NodeSelectorTerm>
                                    {
                                        new V1NodeSelectorTerm
                                        {
                                            MatchExpressions = new List<V1NodeSelectorRequirement>
                                            {
                                                new V1NodeSelectorRequirement
                                                {
                                                    Key = "resource",
                                                    OperatorProperty = "In",
                                                    Values = new List<string> { KubernetesConstants.NodeAffinityValue }
                                                },
                                                new V1NodeSelectorRequirement
                                                {
                                                    Key = "app",
                                                    OperatorProperty = "In",
                                                    Values = new List<string> { KubernetesConstants.NodeAffinityValue }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = containerName,
                                Image = imageName,
                                // Add your specific configuration here based on index
                                // For example, you can mount different config files as volumes
                                // or set different environment variables
                                Ports = new List<V1ContainerPort>
                                {
                                    new V1ContainerPort(containerPort)
                                },
                                VolumeMounts = new List<V1VolumeMount>
                                {
                                    new V1VolumeMount
                                    {
                                        Name = "config-volume",
                                        MountPath = KubernetesConstants
                                            .AppSettingFileMountPath, // Change to the directory where you want to mount
                                        SubPath = KubernetesConstants.AppSettingFileName
                                    },
                                    new V1VolumeMount
                                    {
                                        Name = "log-volume",
                                        MountPath = KubernetesConstants.AppLogFileMountPath
                                    }
                                }
                            },
                            new V1Container
                            {
                                Name = "filebeat-sidecar",
                                Image = KubernetesConstants.FileBeatImage,
                                Args = new List<string>
                                {
                                    "-c", KubernetesConstants.FileBeatConfigMountPath,
                                    "-e",
                                },
                                VolumeMounts = new List<V1VolumeMount>
                                {
                                    new V1VolumeMount
                                    {
                                        Name = "log-volume",
                                        MountPath = KubernetesConstants.AppLogFileMountPath
                                    },
                                    new V1VolumeMount
                                    {
                                        Name = "sidecar-config-volume",
                                        MountPath = KubernetesConstants.FileBeatConfigMountPath,
                                        SubPath = KubernetesConstants.FileBeatConfigFileName
                                    }
                                }
                            }
                        },
                        Volumes = new List<V1Volume>
                        {
                            new V1Volume
                            {
                                Name = "config-volume",
                                ConfigMap = new V1ConfigMapVolumeSource
                                {
                                    Name = configMapName,
                                    Items = new List<V1KeyToPath>
                                    {
                                        new V1KeyToPath
                                        {
                                            Key = KubernetesConstants.AppSettingFileName,
                                            Path = KubernetesConstants.AppSettingFileName
                                        }
                                    }
                                }
                            },
                            new V1Volume
                            {
                                Name = "sidecar-config-volume",
                                ConfigMap = new V1ConfigMapVolumeSource
                                {
                                    Name = sideCarConfigMapName,
                                    Items = new List<V1KeyToPath>
                                    {
                                        new V1KeyToPath
                                        {
                                            Key = KubernetesConstants.FileBeatConfigFileName,
                                            Path = KubernetesConstants.FileBeatConfigFileName
                                        }
                                    }
                                }
                            },
                            new V1Volume
                            {
                                Name = "log-volume",
                                EmptyDir = new V1EmptyDirVolumeSource()
                                // PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
                                // {
                                //     ClaimName = logPVCName
                                // }
                            }
                        }
                    }
                }
            }
        };

        return deployment;
    }
}