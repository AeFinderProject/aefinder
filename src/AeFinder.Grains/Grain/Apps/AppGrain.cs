using AeFinder.Apps;
using AeFinder.Grains.State.Apps;
using Orleans;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Apps;

public class AppGrain : AeFinderGrain<AppState>, IAppGrain
{
    private readonly IObjectMapper _objectMapper;

    public AppGrain(IObjectMapper objectMapper)
    {
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
}