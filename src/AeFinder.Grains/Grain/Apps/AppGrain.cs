using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AeFinder.Grains.State.Apps;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Apps;

public class AppGrain : AeFinderGrain<AppState>, IAppGrain
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;

    public AppGrain(IDistributedEventBus distributedEventBus, IObjectMapper objectMapper)
    {
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
    }

    public async Task<AppDto> CreateAsync(CreateAppDto dto)
    {
        await ReadStateAsync();
        
        if (!State.AppId.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException($"App: {dto.AppName} already exists!");
        }

        var organizationAppGain =
            GrainFactory.GetGrain<IOrganizationAppGrain>(
                GrainIdHelper.GenerateOrganizationAppGrainId(dto.OrganizationId));
        await organizationAppGain.AddAppAsync(dto.AppId);

        State = _objectMapper.Map<CreateAppDto, AppState>(dto);
        State.Status = AppStatus.UnDeployed;
        State.CreateTime = DateTime.UtcNow;
        State.UpdateTime = State.CreateTime;
        await WriteStateAsync();
        
        //Publish app create eto to background worker
        var appCreateEto = _objectMapper.Map<AppState, AppCreateEto>(State);
        await _distributedEventBus.PublishAsync(appCreateEto);
        
        return _objectMapper.Map<AppState, AppDto>(State);
    }

    public async Task<AppDto> UpdateAsync(UpdateAppDto dto)
    {
        await ReadStateAsync();
        
        State.Description = dto.Description;
        State.ImageUrl = dto.ImageUrl;
        State.SourceCodeUrl = dto.SourceCodeUrl;
        State.UpdateTime = DateTime.UtcNow;
        await WriteStateAsync();
        
        //Publish app update eto to background worker
        var appUpdateEto = _objectMapper.Map<AppState, AppUpdateEto>(State);
        await _distributedEventBus.PublishAsync(appUpdateEto);
        
        return _objectMapper.Map<AppState, AppDto>(State);
    }

    public async Task SetStatusAsync(AppStatus status)
    {
        await ReadStateAsync();
        
        State.Status = status;
        State.UpdateTime = DateTime.UtcNow;
        await WriteStateAsync();
    }

    public async Task<AppDto> GetAsync()
    {
        await ReadStateAsync();
        
        return _objectMapper.Map<AppState, AppDto>(State);
    }

    public async Task<string> GetOrganizationIdAsync()
    {
        if (State.OrganizationId.IsNullOrEmpty())
        {
            return State.OrganizationId;
        }
        Guid guid = Guid.ParseExact(State.OrganizationId, "N");
        return guid.ToString();
    }

    public async Task FreezeAppAsync()
    {
        await ReadStateAsync();
        State.Status = AppStatus.Frozen;
        await WriteStateAsync();
    }

    public async Task UnFreezeAppAsync()
    {
        await ReadStateAsync();
        State.Status = AppStatus.UnDeployed;
        await WriteStateAsync();
    }
    
    public async Task ClearGrainStateAsync()
    {
        //Publish app obliterate eto to background worker
        await _distributedEventBus.PublishAsync(new AppObliterateEto()
        {
            AppId = State.AppId
        });
        
        await base.ClearStateAsync();
        DeactivateOnIdle();
    }
}