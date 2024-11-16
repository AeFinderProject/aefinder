using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps.Dto;

namespace AeFinder.Metrics;

public class DefaultKubernetesAppMonitor:IKubernetesAppMonitor
{
    public Task<List<AppPodResourceInfoDto>> GetAppPodsResourceInfoFromPrometheusAsync(List<string> podsName)
    {
        return Task.FromResult(new List<AppPodResourceInfoDto>());
    }
}