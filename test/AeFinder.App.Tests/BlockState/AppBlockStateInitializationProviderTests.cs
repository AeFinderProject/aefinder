using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.MockApp;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Grains.State.BlockStates;
using AeFinder.Sdk;
using Shouldly;
using Xunit;

namespace AeFinder.App.BlockState;

public class AppBlockStateInitializationProviderTests : AeFinderAppTestBase
{
    private readonly IAppBlockStateInitializationProvider _appBlockStateInitializationProvider;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly IAppDataIndexManagerProvider _appDataIndexManagerProvider;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppDataIndexProvider<BlockEntity> _appDataIndexProvider;
    private readonly IAppBlockStateChangeProvider _appBlockStateChangeProvider;
    private readonly IReadOnlyRepository<BlockEntity> _repository;

    public AppBlockStateInitializationProviderTests()
    {
        _appBlockStateInitializationProvider = GetRequiredService<IAppBlockStateInitializationProvider>();
        _appInfoProvider = GetRequiredService<IAppInfoProvider>();
        _appDataIndexManagerProvider = GetRequiredService<IAppDataIndexManagerProvider>();
        _appBlockStateSetProvider = GetRequiredService<IAppBlockStateSetProvider>();
        _appDataIndexProvider = GetRequiredService<IAppDataIndexProvider<BlockEntity>>();
        _appBlockStateChangeProvider = GetRequiredService<IAppBlockStateChangeProvider>();
        _repository = GetRequiredService<IReadOnlyRepository<BlockEntity>>();
    }

    [Fact]
    public async Task Test()
    {
        _appInfoProvider.SetAppId("TestAppId");
        _appInfoProvider.SetVersion("TestVersion");
        var chainId = "AELF";
        
        var appSubscriptionGrain =
            Cluster.Client.GetGrain<IAppSubscriptionGrain>(
                GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId));
        var addSubscriptionDto = await appSubscriptionGrain.AddSubscriptionAsync(new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = false,
                    StartBlockNumber = 100
                }
            }
        }, new byte[] { });
        _appInfoProvider.SetVersion(addSubscriptionDto.NewVersion);

        await _appBlockStateInitializationProvider.InitializeAsync();
        
        var indexName = $"{_appInfoProvider.AppId}-{_appInfoProvider.Version}.{typeof(BlockEntity).Name}".ToLower();

        var id = "AELF-MyEntity";
        await _appDataIndexProvider.AddOrUpdateAsync(new BlockEntity
        {
            Id = id
        }, indexName);
        await _appDataIndexManagerProvider.SavaDataAsync();

        var query =await _repository.GetQueryableAsync();
        var entities = query.ToList(); 
        entities.Count.ShouldBe(1);

        var bestChainBlockHash = "bestChainBlockHash";
        var bestChainBlockHeight = 100L;
        await _appBlockStateChangeProvider.AddBlockStateChangeAsync(chainId,
            new Dictionary<long, List<BlockStateChange>>
            {
                {
                    bestChainBlockHeight,
                    new List<BlockStateChange>
                    {
                        new BlockStateChange
                        {
                            Key = id, Type = $"{typeof(BlockEntity).FullName},{typeof(BlockEntity).Assembly.FullName}",
                        }
                    }
                }
            });


        var appBlockStateSetStatusGrain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
                chainId));
        await appBlockStateSetStatusGrain.SetBlockStateSetStatusAsync(new BlockStateSetStatus
        {
            BestChainBlockHash = bestChainBlockHash,
            BestChainHeight = bestChainBlockHeight
        });
        
        await _appBlockStateInitializationProvider.InitializeAsync();
        query =await _repository.GetQueryableAsync();
        entities = query.ToList(); 
        entities.Count.ShouldBe(0);
    }
}