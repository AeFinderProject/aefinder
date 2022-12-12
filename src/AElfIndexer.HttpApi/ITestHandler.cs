using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.BlockScan;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer;

public interface ITestHandler
{
    Task HandleAsync(SubscribedBlockDto blocks, StreamSequenceToken? token = null);

}

public class TestHandler : ITestHandler, ISingletonDependency
{
    private readonly IBlockScanAppService _blockScanAppService;

    public TestHandler(IBlockScanAppService blockScanAppService)
    {
        _blockScanAppService = blockScanAppService;
    }

    public ILogger<TestHandler> Logger { get; set; }
    
    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken? token = null)
    {
        var clientVersion = await _blockScanAppService.GetClientVersionAsync(subscribedBlock.ClientId);
        if (subscribedBlock.Version != clientVersion.CurrentVersion &&
            subscribedBlock.Version != clientVersion.NewVersion)
        {
            return;
        }
        
        Logger.LogInformation($"========= Version: {subscribedBlock.Version} Height: from {subscribedBlock.Blocks.First().BlockHeight} to {subscribedBlock.Blocks.Last().BlockHeight}");
        
        // var connection = _connectionProvider.GetConnectionByClientId(subscribedBlock.ClientId);
        // if (connection != null && connection.Version == subscribedBlock.Version)
        // {
        //     Logger.LogInformation(
        //         $"Receive Block {subscribedBlock.ClientId} From {subscribedBlock.Blocks.First().BlockHeight} To {subscribedBlock.Blocks.Last().BlockHeight}");
        //     await _hubContext.Clients.Client(connection.ConnectionId).SendAsync("ReceiveBlock", subscribedBlock.Blocks);
        // }
    }

}