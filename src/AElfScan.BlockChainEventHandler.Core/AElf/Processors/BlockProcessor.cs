using AElfScan.AElf.ETOs;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElfScan.AElf.Processors;

public class BlockProcessor:IDistributedEventHandler<BlockChainDataEto>,ITransientDependency
{
    private readonly IAElfAppService _aElfAppService;
    private readonly ILogger<BlockProcessor> _logger;

    public BlockProcessor(IAElfAppService aElfAppService,
        ILogger<BlockProcessor> logger)
    {
        _aElfAppService = aElfAppService;
        _logger = logger;
    }

    public async Task HandleEventAsync(BlockChainDataEto eventData)
    {
        await Task.CompletedTask;
    }
}