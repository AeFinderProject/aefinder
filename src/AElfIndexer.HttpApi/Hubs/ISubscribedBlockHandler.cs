using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Orleans.EventSourcing.Grain.BlockScan;
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
    public ILogger<SubscribedBlockHandler> Logger { get; set; }

    public SubscribedBlockHandler(IConnectionProvider connectionProvider, IHubContext<BlockHub> hubContext)
    {
        _connectionProvider = connectionProvider;
        _hubContext = hubContext;
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken? token = null)
    {
        var connection = _connectionProvider.GetConnectionByClientId(subscribedBlock.ClientId);
        if (connection != null && connection.Version == subscribedBlock.Version)
        {
            Logger.LogInformation(
                $"Receive Block {subscribedBlock.ClientId} From {subscribedBlock.Blocks.First().BlockHeight} To {subscribedBlock.Blocks.Last().BlockHeight}");
            await _hubContext.Clients.Client(connection.ConnectionId).SendAsync("ReceiveBlock", subscribedBlock.Blocks);
        }
    }
}