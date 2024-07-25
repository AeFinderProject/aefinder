using System;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Options;
using Microsoft.Extensions.Options;
using Orleans;

namespace AeFinder.Apps;

public class AppResourceLimitProvider : IAppResourceLimitProvider
{
    private readonly OperationLimitOptions _operationLimitOptions;
    private readonly IClusterClient _clusterClient;
    private readonly KubernetesOptions _kubernetesOptions;

    public AppResourceLimitProvider(IOptionsSnapshot<OperationLimitOptions> operationLimitOptions,
        IClusterClient clusterClient, IOptionsSnapshot<KubernetesOptions> kubernetesOptions)
    {
        _operationLimitOptions = operationLimitOptions.Value;
        _clusterClient = clusterClient;
        _kubernetesOptions = kubernetesOptions.Value;
    }

    public async Task<int> GetMaxEntityCallCountAsync(string appId)
    {
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var resourceLimitDto = await appResourceLimitGrain.GetAsync();
        //Use grain value first
        if (resourceLimitDto.MaxEntityCallCount > 0)
        {
            return resourceLimitDto.MaxEntityCallCount;
        }

        return _operationLimitOptions.MaxEntityCallCount;
    }

    public async Task<int> GetMaxEntitySizeAsync(string appId)
    {
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var resourceLimitDto = await appResourceLimitGrain.GetAsync();
        //Use grain value first
        if (resourceLimitDto.MaxEntitySize > 0)
        {
            return resourceLimitDto.MaxEntitySize;
        }

        return _operationLimitOptions.MaxEntitySize;
    }

    public async Task<int> GetMaxLogCallCountAsync(string appId)
    {
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var resourceLimitDto = await appResourceLimitGrain.GetAsync();
        //Use grain value first
        if (resourceLimitDto.MaxLogCallCount > 0)
        {
            return resourceLimitDto.MaxLogCallCount;
        }

        return _operationLimitOptions.MaxLogCallCount;
    }

    public async Task<int> GetMaxLogSizeAsync(string appId)
    {
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var resourceLimitDto = await appResourceLimitGrain.GetAsync();
        //Use grain value first
        if (resourceLimitDto.MaxLogSize > 0)
        {
            return resourceLimitDto.MaxLogSize;
        }

        return _operationLimitOptions.MaxLogSize;
    }

    public async Task<int> GetMaxContractCallCountAsync(string appId)
    {
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var resourceLimitDto = await appResourceLimitGrain.GetAsync();
        //Use grain value first
        if (resourceLimitDto.MaxContractCallCount > 0)
        {
            return resourceLimitDto.MaxContractCallCount;
        }

        return _operationLimitOptions.MaxContractCallCount;
    }

    public async Task<string> GetAppFullPodRequestCpuCoreAsync(string appId)
    {
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var resourceLimitDto = await appResourceLimitGrain.GetAsync();
        //Use grain value first
        if (!resourceLimitDto.AppFullPodRequestCpuCore.IsNullOrEmpty())
        {
            return resourceLimitDto.AppFullPodRequestCpuCore;
        }

        return _kubernetesOptions.AppFullPodRequestCpuCore;
    }

    public async Task<string> GetAppFullPodRequestMemoryAsync(string appId)
    {
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var resourceLimitDto = await appResourceLimitGrain.GetAsync();
        //Use grain value first
        if (!resourceLimitDto.AppFullPodRequestMemory.IsNullOrEmpty())
        {
            return resourceLimitDto.AppFullPodRequestMemory;
        }

        return _kubernetesOptions.AppFullPodRequestMemory;
    }

    public async Task<string> GetAppQueryPodRequestCpuCoreAsync(string appId)
    {
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var resourceLimitDto = await appResourceLimitGrain.GetAsync();
        //Use grain value first
        if (!resourceLimitDto.AppQueryPodRequestCpuCore.IsNullOrEmpty())
        {
            return resourceLimitDto.AppQueryPodRequestCpuCore;
        }

        return _kubernetesOptions.AppQueryPodRequestCpuCore;
    }

    public async Task<string> GetAppQueryPodRequestMemoryAsync(string appId)
    {
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var resourceLimitDto = await appResourceLimitGrain.GetAsync();
        //Use grain value first
        if (!resourceLimitDto.AppQueryPodRequestMemory.IsNullOrEmpty())
        {
            return resourceLimitDto.AppQueryPodRequestMemory;
        }

        return _kubernetesOptions.AppQueryPodRequestMemory;
    }
}