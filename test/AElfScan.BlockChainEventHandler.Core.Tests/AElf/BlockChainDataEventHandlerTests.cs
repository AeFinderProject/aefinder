using AElfScan.AElf.DTOs;
using AElfScan.AElf.Processors;
using AElfScan.Grain;
using AElfScan.TestInfrastructure;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace AElfScan.BlockChainEventHandler.Core.Tests.AElf;

[Collection(ClusterCollection.Name)]
public sealed class BlockChainDataEventHandlerTests:AElfScanBlockChainEventHandlerCoreTestBase
{
    private readonly IDistributedEventHandler<BlockChainDataEto> _blockChainDataEventHandler;
    private readonly TestCluster _cluster;

    public BlockChainDataEventHandlerTests(ClusterFixture fixture)
    {
        _blockChainDataEventHandler = GetRequiredService<BlockChainDataEventHandler>();
        _cluster = fixture.Cluster;
    }

    public BlockChainDataEto MockBasicEtoData(long blockNumber)
    {
        var blockChainDataEto = new BlockChainDataEto
        {
            ChainId = "AELF",
                Blocks = new List<BlockEto>()
                {
                    new BlockEto()
                    {
                        BlockHash = "3de406161fb47785641612e953973de8a018003065633ce52973378f31240456",
                        BlockNumber = blockNumber,
                        BlockTime = DateTime.Now,
                        PreviousBlockHash = "19456c0236cac35c097bd46c44ae3492a4f4842d6cc19ff594785ec7ccea6460",
                        Signature = "0b1eec144cdf8575f3004352811123a408c505d8f20084bad27bb2aa16cf797a68078fb06a4706207874b0328096d0e03cde427bdcc1605519b2ec277853cb2f01",
                        SignerPubkey = "04bcd1c887cd0edbd4ccf8d9d2b3f72e72511aa6183199600313687ba6c583f13c3d6d716fa40df8604aaed0fcab31135fe3c2d45c009800c075254a3782b4c4db",
                        ExtraProperties = new Dictionary<string, string>()
                        {
                            ["Version"]="0",
                            ["Bloom"]="AAAAAAAAAAAAAAAAAAAAAAEBAAAAAAAAAAAAAAEEAACAAAAAEAAAAAAAAgAAAAAAAAQAAAAAAABAAAAAAAAAAAAAAAAAAAgAQAAAAAAEAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAIBAAAAABAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAAAAAAAAAAAAAAAAAAAAAEAABAAAAAAAAAAAAAAAAAAAAgAAAAAEAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAgCAAAAAAAAAAAAA==",
                            ["ExtraData"]="{ \"chainId\": 9992731, \"previousBlockHash\": \"19456c0236cac35c097bd46c44ae3492a4f4842d6cc19ff594785ec7ccea6460\", \"merkleTreeRootOfTransactions\": \"92656d4668632b738f4bb71a22903d3089287a6c213b45fc4633366f2af929cf\", \"merkleTreeRootOfWorldState\": \"97dad6a116dc008692c351066f15dd0f9ad11f7de21fd2ca785b18bc64ad9467\", \"bloom\": \"AAAAAAAAAAAAAAAAAAAAAAEBAAAAAAAAAAAAAAEEAACAAAAAEAAAAAAAAgAAAAAAAAQAAAAAAABAAAAAAAAAAAAAAAAAAAgAQAAAAAAEAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAIBAAAAABAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAAAAAAAAAAAAAAAAAAAAAEAABAAAAAAAAAAAAAAAAAAAAgAAAAAEAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAgCAAAAAAAAAAAAA==\", \"height\": \"3695\", \"extraData\": { \"CrossChain\": \"\", \"Consensus\": \"CkEEvNHIh80O29TM+NnSs/cuclEaphgxmWADE2h7psWD8Tw9bXFvpA34YEqu0PyrMRNf48LUXACYAMB1JUo3grTE2xKIBAiCAhL8AwqCATA0YmNkMWM4ODdjZDBlZGJkNGNjZjhkOWQyYjNmNzJlNzI1MTFhYTYxODMxOTk2MDAzMTM2ODdiYTZjNTgzZjEzYzNkNmQ3MTZmYTQwZGY4NjA0YWFlZDBmY2FiMzExMzVmZTNjMmQ0NWMwMDk4MDBjMDc1MjU0YTM3ODJiNGM0ZGIS9AIIARABIiIKIIgtjhKXmcTFK25PS8n0NY7+sf4aT8uEnW2MuWIyrcx5KiIKIEzHge8PatqdOJiQ2uAQOc32HTIRrfN+t2Rm9OEqL7pkOO4cSoIBMDRiY2QxYzg4N2NkMGVkYmQ0Y2NmOGQ5ZDJiM2Y3MmU3MjUxMWFhNjE4MzE5OTYwMDMxMzY4N2JhNmM1ODNmMTNjM2Q2ZDcxNmZhNDBkZjg2MDRhYWVkMGZjYWIzMTEzNWZlM2MyZDQ1YzAwOTgwMGMwNzUyNTRhMzc4MmI0YzRkYlIiCiBYeXTie7SLk0wShRB9HOOz9yiRa23Wd3gPvzT4+IhHCVgBYAFqBgjA9aSZBmoMCMD1pJkGEJiS35IBagwIwPWkmQYQqMuu+AFqDAjA9aSZBhDYgoneAmoMCMD1pJkGEJCJj80DagsIwfWkmQYQ8IyYXWoMCMH1pJkGEIiV49sBagwIwfWkmQYQgI+awwJqBgjE9aSZBoABCYgB7xxQxPWkmQY=\", \"SystemTransactionCount\": \"CAI=\" }, \"time\": \"2022-09-20T04:00:04Z\", \"merkleTreeRootOfTransactionStatus\": \"2dd62bb8acffbec4f5a511d4027a3ca075af4ae505fd8349a56d3e36a7fa7d50\", \"signerPubkey\": \"BLzRyIfNDtvUzPjZ0rP3LnJRGqYYMZlgAxNoe6bFg/E8PW1xb6QN+GBKrtD8qzETX+PC1FwAmADAdSVKN4K0xNs=\", \"signature\": \"Cx7sFEzfhXXzAENSgREjpAjFBdjyAIS60nuyqhbPeXpoB4+wakcGIHh0sDKAltDgPN5Ce9zBYFUZsuwneFPLLwE=\" }",
                            ["MerkleTreeRootOfTransactions"]="92656d4668632b738f4bb71a22903d3089287a6c213b45fc4633366f2af929cf",
                            ["MerkleTreeRootOfWorldState"]="97dad6a116dc008692c351066f15dd0f9ad11f7de21fd2ca785b18bc64ad9467"
                        },
                        Transactions = new List<TransactionEto>(){}
                    }
                }
        };

        return blockChainDataEto;
    }

    [Fact]
    public async Task test()
    {
        var blockChainDataEto = MockBasicEtoData(10);

        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto);
        
        var grain = _cluster.GrainFactory.GetGrain<IBlockGrain>(48);

        var blockDictionary = await grain.GetBlockDictionary();
        blockDictionary.ShouldContainKey("3de406161fb47785641612e953973de8a018003065633ce52973378f31240456");
    }
}