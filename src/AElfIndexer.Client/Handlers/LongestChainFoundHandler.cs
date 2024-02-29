using AElfIndexer.Client.BlockState;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElfIndexer.Client.Handlers;

public class LongestChainFoundHandler : ILocalEventHandler<LongestChainFoundEventData>,
    ITransientDependency
{
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppStateProvider _appStateProvider;

    public LongestChainFoundHandler(IAppBlockStateSetProvider appBlockStateSetProvider,
        IAppStateProvider appStateProvider)
    {
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _appStateProvider = appStateProvider;
    }

    public async Task HandleEventAsync(LongestChainFoundEventData eventData)
    {
        
    }
}