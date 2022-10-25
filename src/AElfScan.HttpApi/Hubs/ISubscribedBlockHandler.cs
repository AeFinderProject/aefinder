using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScan.Orleans.EventSourcing.Grain.ScanClients;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Hubs;

public interface ISubscribedBlockHandler
{
    Task HandleAsync(SubscribedBlockDto blocks, StreamSequenceToken? token = null);
}

public class SubscribedBlockHandler : ISubscribedBlockHandler, ISingletonDependency
{
    private readonly IConnectionProvider _connectionProvider;
    private readonly IHubContext<BlockHub> _hubContext;
    public ILogger<SubscribedBlockHandler> Logger { get; set; }

    public SubscribedBlockHandler(IConnectionProvider connectionProvider, IHubContext<BlockHub> hubContext)
    {
        _connectionProvider = connectionProvider;
        _hubContext = hubContext;
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken? token = null)
    {
        var connectionId = _connectionProvider.GetConnectionId(subscribedBlock.ClientId);
        if (connectionId != null)
        {
            // TODO: check version
            Logger.LogDebug(
                $"Receive Block {subscribedBlock.ClientId} From {subscribedBlock.Blocks.First().BlockHeight} To {subscribedBlock.Blocks.Last().BlockHeight}");
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveBlock", subscribedBlock.Blocks);
        }
    }
}