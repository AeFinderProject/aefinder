using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps.Dto;

namespace AeFinder.Metrics;

public interface IKubernetesAppMonitor
{
    Task<List<AppPodResourceInfoDto>> GetAppAllPodResourcesAsync();
}