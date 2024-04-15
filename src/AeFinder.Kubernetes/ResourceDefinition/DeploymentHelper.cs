using k8s.Models;

namespace AeFinder.Kubernetes.ResourceDefinition;

public class DeploymentHelper
{
    public static string GetAppDeploymentName(string appId, string version, string clientType)
    {
        return $"deployment-{appId}-{version}-{clientType}".ToLower();
    }

    public static V1Deployment CreateAppDeploymentWithFileBeatSideCarDefinition(string imageName, string deploymentName,
        int replicasCount, string containerName, string configMapName,string sideCarConfigMapName)
    {
        var labels = new Dictionary<string, string>
        {
            { KubernetesConstants.AppLabelKey, deploymentName }
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
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = containerName,
                                Image = imageName,
                                // Add your specific configuration here based on index
                                // For example, you can mount different config files as volumes
                                // or set different environment variables

                                VolumeMounts = new List<V1VolumeMount>
                                {
                                    new V1VolumeMount
                                    {
                                        Name = "config-volume",
                                        MountPath = KubernetesConstants.AppSettingFileMountPath, // Change to the directory where you want to mount
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