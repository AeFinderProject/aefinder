using AeFinder.Apps;
using AeFinder.Grains.State.Apps;
using Orleans;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Apps;

public class AppGrain : Grain<AppState>, IAppGrain
{
    private readonly IObjectMapper _objectMapper;

    public AppGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<AppDto> CreateAsync(CreateAppDto dto)
    {
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
        return _objectMapper.Map<AppState, AppDto>(State);
    }

    public async Task<AppDto> UpdateAsync(UpdateAppDto dto)
    {
        State.Description = dto.Description;
        State.ImageUrl = dto.ImageUrl;
        State.SourceCodeUrl = dto.SourceCodeUrl;
        State.UpdateTime = DateTime.UtcNow;
        await WriteStateAsync();
        
        return _objectMapper.Map<AppState, AppDto>(State);
    }

    public Task<AppDto> GetAsync()
    {
        return Task.FromResult(_objectMapper.Map<AppState, AppDto>(State));
    }
    
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }
    
    
    
    
    
    // public async Task<RegisterDto> Register(string adminId, string appId, string name)
    // {
    //     if (!string.IsNullOrWhiteSpace(State.AdminId))
    //     {
    //         return new RegisterDto() { Success = State.AdminId == adminId && State.Name.Equals(name), Added = false };
    //     }
    //
    //     State.AdminId = adminId;
    //     State.AppId = appId;
    //     State.Name = name;
    //     State.AeFinderAppInfo = new AeFinderAppInfo() { AppId = appId, Name = name };
    //     await WriteStateAsync();
    //     return new RegisterDto() { Success = true, Added = true };
    // }
    //
    // public async Task<AppInfo> AddDeveloperToApp(string developerId)
    // {
    //     var appInfo = new AppInfo() { AeFinderAppInfo = State.AeFinderAppInfo, DeveloperIds = State.DeveloperIds, AppId = State.AppId };
    //     if (developerId.IsNullOrEmpty())
    //     {
    //         return appInfo;
    //     }
    //
    //     State.DeveloperIds.Add(developerId);
    //     await WriteStateAsync();
    //     return appInfo;
    // }
    //
    // public Task<bool> IsDeveloper(string developerId)
    // {
    //     return Task.FromResult(State.DeveloperIds.Contains(developerId));
    // }
    //
    // public async Task<AppInfo> AddOrUpdateAppInfo(AeFinderAppInfo aeFinderAppInfo)
    // {
    //     if (State.AppId.IsNullOrEmpty())
    //     {
    //         return null;
    //     }
    //
    //     aeFinderAppInfo.AppId = State.AppId;
    //     aeFinderAppInfo.Name = State.AppId;
    //     State.AeFinderAppInfo = aeFinderAppInfo;
    //
    //     await WriteStateAsync();
    //     return new AppInfo() { AeFinderAppInfo = State.AeFinderAppInfo, DeveloperIds = State.DeveloperIds, AppId = State.AppId };
    // }
    //
    //
    // public Task<AeFinderAppInfo> GetAppInfo()
    // {
    //     return Task.FromResult(State.AeFinderAppInfo);
    // }
    //
    // public Task<bool> IsAdmin(string appId)
    // {
    //     return Task.FromResult(!appId.IsNullOrEmpty() && State.AppId.IsNullOrEmpty() && appId.Equals(State.AdminId));
    // }

}