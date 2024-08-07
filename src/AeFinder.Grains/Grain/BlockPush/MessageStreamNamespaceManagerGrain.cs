using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace AeFinder.Grains.Grain.BlockPush;

public class MessageStreamNamespaceManagerGrain : Orleans.Grain, IMessageStreamNamespaceManagerGrain
{
    private readonly ConcurrentDictionary<string, int> _messageStreamNamespaceAppCount = new();

    private readonly BlockPushOptions _blockPushOptions;

    public MessageStreamNamespaceManagerGrain(IOptionsSnapshot<BlockPushOptions> blockPushOptions)
    {
        _blockPushOptions = blockPushOptions.Value;
    }

    public async Task<string> GetMessageStreamNamespaceAsync(string appId)
    {
        return await GetMessageStreamNamespaceAsync(appId, _messageStreamNamespaceAppCount);
    }

    private async Task<string> GetMessageStreamNamespaceAsync(string appId, ConcurrentDictionary<string, int> namespaceAppCount)
    {
        if (namespaceAppCount.Count == 0)
        {
            return AeFinderApplicationConsts.MessageStreamNamespace;
        }

        foreach (var n in namespaceAppCount)
        {
            var grain = GrainFactory.GetGrain<IMessageStreamNamespaceGrain>(n.Key);
            var contains = await grain.ContainsAppAsync(appId);
            if (contains)
            {
                return n.Key;
            }
        }

        var streamNamespce = namespaceAppCount.MinBy(o => o.Value);
        var streamNamespceGrain = GrainFactory.GetGrain<IMessageStreamNamespaceGrain>(streamNamespce.Key);
        await streamNamespceGrain.AddAppAsync(appId);
        namespaceAppCount[streamNamespce.Key] = streamNamespce.Value + 1;
        return streamNamespce.Key;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var tasks = _blockPushOptions.MessageStreamNamespaces.Select(async o =>
        {
            var grain = GrainFactory.GetGrain<IMessageStreamNamespaceGrain>(o);
            var appCount = await grain.GetAppCountAsync();
            _messageStreamNamespaceAppCount[o] = appCount;
        }).ToList();

        await Task.WhenAll(tasks);
    }
}