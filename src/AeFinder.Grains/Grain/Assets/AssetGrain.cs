using AeFinder.Assets;
using AeFinder.Grains.Grain.Merchandises;
using AeFinder.Grains.State.Assets;
using AeFinder.Merchandises;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Assets;

public class AssetGrain : AeFinderGrain<AssetState>, IAssetGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;

    public AssetGrain(IObjectMapper objectMapper, IDistributedEventBus distributedEventBus)
    {
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<AssetState> CreateAssetAsync(Guid id, Guid organizationId, CreateAssetInput input)
    {
        State = _objectMapper.Map<CreateAssetInput, AssetState>(input);
        State.Id = id;
        State.OrganizationId = organizationId;
        State.Status = AssetStatus.Unused;
        await WriteStateAsync();

        return State;
    }

    public async Task<AssetState> GetAsync()
    {
        await ReadStateAsync();
        return State;
    }

    public async Task PayAsync(decimal paidAmount)
    {
        await ReadStateAsync();
        State.PaidAmount = paidAmount;
        await WriteStateAsync();
    }

    public async Task RelateAppAsync(string appId)
    {
        await ReadStateAsync();

        var merchandiseGrain = GrainFactory.GetGrain<IMerchandiseGrain>(State.MerchandiseId);
        var merchandise = await merchandiseGrain.GetAsync();

        if (!State.AppId.IsNullOrEmpty() || merchandise.Category != MerchandiseCategory.Resource)
        {
            throw new UserFriendlyException($"Asset: {State.Id} cannot relate {appId}");
        }

        State.AppId = appId;
        await WriteStateAsync();
    }

    public async Task SuspendAsync()
    {
        await ReadStateAsync();
        State.Status = AssetStatus.Pending;
        await WriteStateAsync();
    }

    public async Task StartUsingAsync(DateTime dateTime)
    {
        await ReadStateAsync();
        if (State.Status != AssetStatus.Unused)
        {
            return;
        }

        State.Status = AssetStatus.Using;
        State.StartTime = dateTime;
        await WriteStateAsync();
    }
    
    public async Task ReleaseAsync(DateTime dateTime)
    {
        await ReadStateAsync();
        State.Status = AssetStatus.Released;
        State.EndTime = dateTime;
        await WriteStateAsync();
    }

    public async Task LockAsync(bool isLock)
    {
        await ReadStateAsync();
        State.IsLocked = isLock;
        await WriteStateAsync();
    }

    protected override async Task WriteStateAsync()
    {
        await PublishEventAsync();
        await base.WriteStateAsync();
    }

    private async Task PublishEventAsync()
    {
        var eventData = _objectMapper.Map<AssetState, AssetChangedEto>(State);
        await _distributedEventBus.PublishAsync(eventData);
    }
}