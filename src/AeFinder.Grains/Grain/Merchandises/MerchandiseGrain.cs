using AeFinder.Grains.State.Merchandises;
using AeFinder.Merchandises;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Merchandises;

public class MerchandiseGrain : AeFinderGrain<MerchandiseState>, IMerchandiseGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;

    public MerchandiseGrain(IObjectMapper objectMapper, IDistributedEventBus distributedEventBus)
    {
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<MerchandiseState> CreateAsync(Guid id, CreateMerchandiseInput input)
    {
        await ReadStateAsync();
        State = _objectMapper.Map<CreateMerchandiseInput, MerchandiseState>(input);
        State.Id = id;
        await WriteStateAsync();

        return State;
    }

    public async Task<MerchandiseState> UpdateAsync(UpdateMerchandiseInput input)
    {
        await ReadStateAsync();
        _objectMapper.Map<UpdateMerchandiseInput, MerchandiseState>(input, State);
        await WriteStateAsync();
        
        return State;
    }

    public async Task<MerchandiseState> GetAsync()
    {
        await ReadStateAsync();
        return State;
    }
    
    protected override async Task WriteStateAsync()
    {
        await PublishEventAsync();
        await base.WriteStateAsync();
    }

    private async Task PublishEventAsync()
    {
        var eventData = _objectMapper.Map<MerchandiseState, MerchandiseChangedEto>(State);
        await _distributedEventBus.PublishAsync(eventData);
    }
}