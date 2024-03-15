using System.Threading.Tasks;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Apps;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Studio;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class StudioService : IStudioService, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<StudioService> _logger;
    private readonly IObjectMapper _objectMapper;

    public StudioService(IClusterClient clusterClient, ILogger<StudioService> logger, IObjectMapper objectMapper)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _objectMapper = objectMapper;
    }

    public async Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(string clientId, string name)
    {
        _logger.LogInformation("request ApplyAeFinderAppName: {0} {1}", clientId, name);
        var nameGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAeFinderNameGrainId(name));
        var res = await nameGrain.Exists(clientId);
        var ans = new ApplyAeFinderAppNameDto() { Success = res.Exists };
        _logger.LogInformation("response ApplyAeFinderAppName: {0} {1} exists={2} added={3}", clientId, name, res.Exists, res.Added);
        if (!res.Added || res.Exists)
        {
            return ans;
        }

        var userAppGrain = _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppGrainId(clientId));
        await userAppGrain.AddAppName(name);
        return ans;
    }

    public async Task<AddOrUpdateAeFinderAppDto> UpdateAeFinderApp(string clientId, AddOrUpdateAeFinderAppInput input)
    {
        var userAppGrain = _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppGrainId(clientId));
        var result = await userAppGrain.AddOrUpdateAppByName(_objectMapper.Map<AddOrUpdateAeFinderAppInput, AeFinderAppInfo>(input));
        return new AddOrUpdateAeFinderAppDto() { Success = result };
    }

    public async Task<AeFinderAppInfoDto> GetAeFinderApp(string clientId, GetAeFinderAppInfoInput input)
    {
        var userAppGrain = _clusterClient.GetGrain<IUserAppGrain>(GrainIdHelper.GenerateUserAppGrainId(clientId));
        var info = await userAppGrain.GetAppByName(input.Name);
        return info == null ? null : _objectMapper.Map<AeFinderAppInfo, AeFinderAppInfoDto>(info);
    }
}