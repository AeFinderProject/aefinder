using System;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Apps.Dto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Options;
using GraphQL;
using Grpc.Net.Client.Balancer;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Apps;

public class AppResourceLimitProvider : IAppResourceLimitProvider, ISingletonDependency
{
    private readonly OperationLimitOptions _operationLimitOptions;
    private readonly IClusterClient _clusterClient;
    private readonly KubernetesOptions _kubernetesOptions;
    private readonly AppDeployOptions _appDeployOptions;
    private readonly IObjectMapper _objectMapper;

    public AppResourceLimitProvider(IOptionsSnapshot<OperationLimitOptions> operationLimitOptions,
        IClusterClient clusterClient, IOptionsSnapshot<KubernetesOptions> kubernetesOptions,
        IObjectMapper objectMapper,IOptionsSnapshot<AppDeployOptions> appDeployOptions)
    {
        _operationLimitOptions = operationLimitOptions.Value;
        _clusterClient = clusterClient;
        _kubernetesOptions = kubernetesOptions.Value;
        _appDeployOptions = appDeployOptions.Value;
        _objectMapper = objectMapper;
    }

    public async Task<AppResourceLimitDto> GetAppResourceLimitAsync(string appId)
    {
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var resourceLimitDto = await appResourceLimitGrain.GetAsync();
        
        //Use grain value first
        if (resourceLimitDto.MaxEntityCallCount <= 0)
        {
            resourceLimitDto.MaxEntityCallCount = _operationLimitOptions.MaxEntityCallCount;
        }
        
        //Use grain value first
        if (resourceLimitDto.MaxEntitySize <= 0)
        {
            resourceLimitDto.MaxEntitySize = _operationLimitOptions.MaxEntitySize;
        }
        
        //Use grain value first
        if (resourceLimitDto.MaxLogCallCount <= 0)
        {
            resourceLimitDto.MaxLogCallCount = _operationLimitOptions.MaxLogCallCount;
        }
        
        //Use grain value first
        if (resourceLimitDto.MaxLogSize <= 0)
        {
            resourceLimitDto.MaxLogSize = _operationLimitOptions.MaxLogSize;
        }
        
        //Use grain value first
        if (resourceLimitDto.MaxContractCallCount <= 0)
        {
            resourceLimitDto.MaxContractCallCount = _operationLimitOptions.MaxContractCallCount;
        }
        
        //Use grain value first
        if (resourceLimitDto.AppFullPodRequestCpuCore.IsNullOrEmpty())
        {
            resourceLimitDto.AppFullPodRequestCpuCore = _kubernetesOptions.AppFullPodRequestCpuCore;
        }
        
        //Use grain value first
        if (resourceLimitDto.AppFullPodRequestMemory.IsNullOrEmpty())
        {
            resourceLimitDto.AppFullPodRequestMemory = _kubernetesOptions.AppFullPodRequestMemory;
        }
        
        //Use grain value first
        if (resourceLimitDto.AppQueryPodRequestCpuCore.IsNullOrEmpty())
        {
            resourceLimitDto.AppQueryPodRequestCpuCore = _kubernetesOptions.AppQueryPodRequestCpuCore;
        }
        
        //Use grain value first
        if (resourceLimitDto.AppQueryPodRequestMemory.IsNullOrEmpty())
        {
            resourceLimitDto.AppQueryPodRequestMemory = _kubernetesOptions.AppQueryPodRequestMemory;
        }

        if (resourceLimitDto.AppPodReplicas <= 0)
        {
            resourceLimitDto.AppPodReplicas = _kubernetesOptions.AppPodReplicas;
        }
        
        if (resourceLimitDto.MaxAppCodeSize <= 0)
        {
            resourceLimitDto.MaxAppCodeSize = _appDeployOptions.MaxAppCodeSize;
        }
        
        if (resourceLimitDto.MaxAppAttachmentSize <= 0)
        {
            resourceLimitDto.MaxAppAttachmentSize = _appDeployOptions.MaxAppAttachmentSize;
        }
        
        //Use grain value first
        if (resourceLimitDto.AppFullPodLimitCpuCore.IsNullOrEmpty())
        {
            resourceLimitDto.AppFullPodLimitCpuCore = String.Empty;
        }
        
        //Use grain value first
        if (resourceLimitDto.AppFullPodLimitMemory.IsNullOrEmpty())
        {
            resourceLimitDto.AppFullPodLimitMemory = String.Empty;
        }

        return resourceLimitDto;
    }

    public async Task SetAppResourceLimitAsync(string appId, AppResourceLimitDto limitDto)
    {
        var appResourceLimitGrain = _clusterClient.GetGrain<IAppResourceLimitGrain>(
            GrainIdHelper.GenerateAppResourceLimitGrainId(appId));
        var setLimitDto = _objectMapper.Map<AppResourceLimitDto, SetAppResourceLimitDto>(limitDto);
        await appResourceLimitGrain.SetAsync(setLimitDto);
    }
}