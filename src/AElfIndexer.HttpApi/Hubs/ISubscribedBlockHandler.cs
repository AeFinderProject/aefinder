using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.BlockScan;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Hubs;

public interface ISubscribedBlockHandler
{
    Task HandleAsync(SubscribedBlockDto blocks, StreamSequenceToken? token = null);
}

public class SubscribedBlockHandler : ISubscribedBlockHandler, ISingletonDependency
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly IHubContext<BlockHub> _hubContext;
    private readonly IBlockScanAppService _blockScanAppService;
    public ILogger<SubscribedBlockHandler> Logger { get; set; }

    public SubscribedBlockHandler(IConnectionProvider connectionProvider, IHubContext<BlockHub> hubContext,
        IBlockScanAppService blockScanAppService)
    {
        _connectionProvider = connectionProvider;
        _hubContext = hubContext;
        _blockScanAppService = blockScanAppService;
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken? token = null)
    {

        var clientVersion = await _blockScanAppService.GetClientVersionAsync(subscribedBlock.ClientId);
        if (subscribedBlock.Version != clientVersion.CurrentVersion &&
            subscribedBlock.Version != clientVersion.NewVersion)
        {
            Logger.LogInformation($"Wrong Version: {subscribedBlock.Version}");
            return;
        }

        var connection = _connectionProvider.GetConnectionByClientId(subscribedBlock.ClientId);

        Logger.LogInformation(
            $"Receive Block {subscribedBlock.ClientId} From {subscribedBlock.Blocks.First().BlockHeight} To {subscribedBlock.Blocks.Last().BlockHeight}");
        await _hubContext.Clients.Client(connection.ConnectionId).SendAsync("ReceiveBlock", subscribedBlock);
    }
}