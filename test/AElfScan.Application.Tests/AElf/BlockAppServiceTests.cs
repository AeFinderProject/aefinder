using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElfScan.AElf.Dtos;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using Shouldly;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace AElfScan.AElf;

public class BlockAppServiceTests:AElfScanApplicationTestBase
{
    private readonly BlockAppService _blockAppService;
    private readonly INESTRepository<BlockIndex, string> _blockIndexRepository;

    public BlockAppServiceTests()
    {
        _blockAppService = GetRequiredService<BlockAppService>();
        _blockIndexRepository = GetRequiredService<INESTRepository<BlockIndex, string>>();
    }

    [Fact]
    public async Task GetBlocksAsync_Test1_4()
    {
        //Unit Test 1
        var block_100 =
            MockDataHelper.MockNewBlockEtoData(100, MockDataHelper.CreateBlockHash(), MockDataHelper.CreateBlockHash());
        await _blockIndexRepository.DeleteAsync(block_100.Id);
        await _blockIndexRepository.AddAsync(block_100);

        GetBlocksInput getBlocksInput_test1 = new GetBlocksInput()
        {
            ChainId = "AElf",
            StartBlockNumber = 100,
            EndBlockNumber = 100,
            HasTransaction = true
        };
        List<BlockDto> blockDtos_test1 =await _blockAppService.GetBlocksAsync(getBlocksInput_test1);
        blockDtos_test1.Count.ShouldBeGreaterThan(0);
        blockDtos_test1[0].BlockNumber.ShouldBe(100);
        
        //Unit Test 2
        GetBlocksInput getBlocksInput_test2 = new GetBlocksInput()
        {
            ChainId = "AElf",
            StartBlockNumber = 100,
            EndBlockNumber = 50
        };
        List<BlockDto> blockDtos_test2 =await _blockAppService.GetBlocksAsync(getBlocksInput_test2);
        blockDtos_test2.Count.ShouldBe(0);
        
        //Unit Test 3
        GetBlocksInput getBlocksInput_test3 = new GetBlocksInput()
        {
            ChainId = "AElf",
            StartBlockNumber = 100,
            EndBlockNumber = 100
        };
        List<BlockDto> blockDtos_test3 =await _blockAppService.GetBlocksAsync(getBlocksInput_test3);
        blockDtos_test3.Count.ShouldBeGreaterThan(0);
        blockDtos_test3[0].BlockNumber.ShouldBe(100);
        
        //Unit Test 4
        var block_200 =
            MockDataHelper.MockNewBlockEtoData(200, MockDataHelper.CreateBlockHash(), MockDataHelper.CreateBlockHash());
        await _blockIndexRepository.DeleteAsync(block_200.Id);
        await _blockIndexRepository.AddAsync(block_200);
        GetBlocksInput getBlocksInput_test4 = new GetBlocksInput()
        {
            ChainId = "AElf",
            StartBlockNumber = 100,
            EndBlockNumber = 300,
            HasTransaction = true
        };
        List<BlockDto> blockDtos_test4 =await _blockAppService.GetBlocksAsync(getBlocksInput_test4);
        blockDtos_test4.Count.ShouldBeGreaterThan(0);
        blockDtos_test4.ShouldContain(x=>x.BlockNumber==100);
        blockDtos_test4.ShouldContain(x=>x.BlockNumber==200);
        blockDtos_test4.ShouldNotContain(x=>x.BlockNumber==300);
    }
}