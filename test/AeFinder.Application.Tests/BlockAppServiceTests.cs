using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AeFinder.Block;
using AeFinder.Block.Dtos;
using AeFinder.Entities.Es;
using AElf.EntityMapping.Repositories;
using Shouldly;
using Xunit;

namespace AeFinder;

public class BlockAppServiceTests:AeFinderApplicationTestBase
{
    private readonly BlockAppService _blockAppService;
    private readonly IEntityMappingRepository<BlockIndex, string> _blockIndexRepository;
    private readonly IEntityMappingRepository<TransactionIndex, string> _transactionIndexRepository;
    private readonly IEntityMappingRepository<LogEventIndex, string> _logEventIndexRepository;

    public BlockAppServiceTests()
    {
        _blockAppService = GetRequiredService<BlockAppService>();
        _blockIndexRepository = GetRequiredService<IEntityMappingRepository<BlockIndex, string>>();
        _transactionIndexRepository = GetRequiredService<IEntityMappingRepository<TransactionIndex, string>>();
        _logEventIndexRepository = GetRequiredService<IEntityMappingRepository<LogEventIndex, string>>();
    }

    private async Task ClearBlockIndex(string chainId,long startBlockNumber,long endBlockNumber)
    {
        Expression<Func<BlockIndex, bool>> expression = p => p.ChainId == chainId && p.BlockHeight >= startBlockNumber && p.BlockHeight <= endBlockNumber;
        var queryable = await _blockIndexRepository.GetQueryableAsync();
        var filterList= queryable.Where(expression).ToList();
        foreach (var deleteBlock in filterList)
        {
            await _blockIndexRepository.DeleteAsync(deleteBlock);
        }
    }
    
    [Fact]
    public async Task ElasticSearchBulkAllIndexTest()
    {
        //clear data for unit test
       // await ClearBlockIndex("AELF", 900, 999);
        
        var block_900 =
            MockDataHelper.MockNewBlockEtoData(900, "900",false);
        var block_901 =
            MockDataHelper.MockNewBlockEtoData(901, "901",false);
        var block_902 =
            MockDataHelper.MockNewBlockEtoData(902, "902",false);
        var block_903 =
            MockDataHelper.MockNewBlockEtoData(903, "903",false);
        List<BlockIndex> blockList = new List<BlockIndex>();
        blockList.Add(block_900);
        await _blockIndexRepository.AddAsync(block_900);
        blockList.Add(block_901);
        await _blockIndexRepository.AddAsync(block_901);
        blockList.Add(block_902);
        await _blockIndexRepository.AddAsync(block_902);
        blockList.Add(block_903); 
        await _blockIndexRepository.AddAsync(block_903);
        //await _blockIndexRepository.BulkAddOrUpdateAsync(blockList);
        
        block_900.Confirmed = true;
        block_901.Confirmed = true;
        block_902.Confirmed = true;
        block_903.Confirmed = true;
        List<BlockIndex> blockList2 = new List<BlockIndex>();
        blockList2.Add(block_900);
        await _blockIndexRepository.AddAsync(block_900);
        blockList2.Add(block_901);
        await _blockIndexRepository.AddAsync(block_901);
        blockList2.Add(block_902);
        await _blockIndexRepository.AddAsync(block_902);
        blockList2.Add(block_903); 
        await _blockIndexRepository.AddAsync(block_903);
        //await _blockIndexRepository.BulkAddOrUpdateAsync(blockList2);
        
    }

    [Fact]
    public async Task ElasticSearchBulkAllDeleteTest()
    {
        //clear data for unit test
        // await ClearBlockIndex("AELF", 0, 99999);
        var block_900 =
            MockDataHelper.MockNewBlockEtoData(900, "900",false);
        var block_901 =
            MockDataHelper.MockNewBlockEtoData(901, "901",false);
        var block_902 =
            MockDataHelper.MockNewBlockEtoData(902, "902",false);
        var block_903 =
            MockDataHelper.MockNewBlockEtoData(903, "903",false);
        var block_3903 =
            MockDataHelper.MockNewBlockEtoData(3903, "3903",false);
        var block_8000 =
            MockDataHelper.MockNewBlockEtoData(8000, "8000",false);
        List<BlockIndex> blockList = new List<BlockIndex>();
        blockList.Add(block_900);
        blockList.Add(block_901);
        blockList.Add(block_902);
        blockList.Add(block_903);
        blockList.Add(block_3903);
        blockList.Add(block_8000);
        var queryable = await _blockIndexRepository.GetQueryableAsync();
        await _blockIndexRepository.AddOrUpdateManyAsync(blockList);
        Thread.Sleep(2000);
        Expression<Func<BlockIndex, bool>> expression = p => p.Id == "903" && p.ChainId == block_900.ChainId;
        var blockIndex_903= queryable.Where(expression).ToList();
        
        blockIndex_903.FirstOrDefault().BlockHash.ShouldBe(block_903.BlockHash);
    }

    [Fact]
    public async Task GetBlocksAsync_Test1_13_30()
    {
        //clear data for unit test
        await ClearBlockIndex("AELF", 100, 300);
        await ClearBlockIndex("AELF", 1000, 5000);
        await ClearBlockIndex("AELF", 0, 99999);
        Thread.Sleep(2000);
        //Unit Test 1
        var block_100 =
            MockDataHelper.MockNewBlockEtoData(100, MockDataHelper.CreateBlockHash(),false);
        // block_100.Transactions = new List<Transaction>();
        await _blockIndexRepository.AddAsync(block_100);
        Thread.Sleep(2000);
        GetBlocksInput getBlocksInput_test1 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 100,
            HasTransaction = true
        };
        List<BlockDto> blockDtos_test1 = await _blockAppService.GetBlocksAsync(getBlocksInput_test1);
        blockDtos_test1.Count.ShouldBeGreaterThan(0);
        blockDtos_test1[0].BlockHeight.ShouldBe(100);
        // blockDtos_test1.ShouldAllBe(x => x.Transactions.Count == 0);
        
        //Unit Test 2
        GetBlocksInput getBlocksInput_test2 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 50
        };
        List<BlockDto> blockDtos_test2 =await _blockAppService.GetBlocksAsync(getBlocksInput_test2);
        blockDtos_test2.Count.ShouldBe(0);
        
        //Unit Test 3
        GetBlocksInput getBlocksInput_test3 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 100
        };
        List<BlockDto> blockDtos_test3 =await _blockAppService.GetBlocksAsync(getBlocksInput_test3);
        blockDtos_test3.Count.ShouldBeGreaterThan(0);
        blockDtos_test3[0].BlockHeight.ShouldBe(100);
        
        //Unit Test 4
        var block_200 =
            MockDataHelper.MockNewBlockEtoData(200, MockDataHelper.CreateBlockHash(),false);
        await _blockIndexRepository.AddAsync(block_200);
        Thread.Sleep(2000);
        GetBlocksInput getBlocksInput_test4 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 300,
            HasTransaction = true
        };
        List<BlockDto> blockDtos_test4 =await _blockAppService.GetBlocksAsync(getBlocksInput_test4);
        blockDtos_test4.Count.ShouldBeGreaterThan(0);
        blockDtos_test4.ShouldContain(x=>x.BlockHeight==100);
        blockDtos_test4.ShouldContain(x=>x.BlockHeight==200);
        blockDtos_test4.ShouldNotContain(x=>x.BlockHeight==300);
        // blockDtos_test4.ShouldContain(x => x.Transactions.Count > 0);
        
        //Unit Test 5
        var block_180 = MockDataHelper.MockNewBlockEtoData(180, MockDataHelper.CreateBlockHash(),true);
        var block_180_fork = MockDataHelper.MockNewBlockEtoData(180, MockDataHelper.CreateBlockHash(),false);
        await _blockIndexRepository.AddAsync(block_180);
        await _blockIndexRepository.AddAsync(block_180_fork);
        Thread.Sleep(2000);
        GetBlocksInput getBlocksInput_test5 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 200,
            HasTransaction = false
        };
        List<BlockDto> blockDtos_test5 =await _blockAppService.GetBlocksAsync(getBlocksInput_test5);
        blockDtos_test5.Count(x=>x.BlockHeight==180).ShouldBe(2);
        // blockDtos_test5.ShouldAllBe(x => x.Transactions == null);
        
        //Unit Test 6
        var block_1000 = MockDataHelper.MockNewBlockEtoData(1000, MockDataHelper.CreateBlockHash(),false);
        var block_1500 = MockDataHelper.MockNewBlockEtoData(1500, MockDataHelper.CreateBlockHash(),false);
        var block_1999 = MockDataHelper.MockNewBlockEtoData(1999, MockDataHelper.CreateBlockHash(),false);
        var block_2000 = MockDataHelper.MockNewBlockEtoData(2000, MockDataHelper.CreateBlockHash(),false);
        var block_3000 = MockDataHelper.MockNewBlockEtoData(3000, MockDataHelper.CreateBlockHash(),false);
        var block_4000 = MockDataHelper.MockNewBlockEtoData(4000, MockDataHelper.CreateBlockHash(),false);
        var block_4999 = MockDataHelper.MockNewBlockEtoData(4999, MockDataHelper.CreateBlockHash(),false);
        var block_5000 = MockDataHelper.MockNewBlockEtoData(5000, MockDataHelper.CreateBlockHash(),false);
        await _blockIndexRepository.AddAsync(block_1000);
        await _blockIndexRepository.AddAsync(block_1500);
        await _blockIndexRepository.AddAsync(block_1999);
        await _blockIndexRepository.AddAsync(block_2000);
        await _blockIndexRepository.AddAsync(block_3000);
        await _blockIndexRepository.AddAsync(block_4000);
        await _blockIndexRepository.AddAsync(block_4999);
        await _blockIndexRepository.AddAsync(block_5000);
        Thread.Sleep(2000);
        GetBlocksInput getBlocksInput_test6 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 1000,
            EndBlockHeight = 5000,
            HasTransaction = true
        };
        List<BlockDto> blockDtos_test6 =await _blockAppService.GetBlocksAsync(getBlocksInput_test6);
        blockDtos_test6.Max(x=>x.BlockHeight).ShouldBe(1999);
        
        //Unit Test 7
        GetBlocksInput getBlocksInput_test7 = new GetBlocksInput()
        {
            ChainId = "AELG",
            StartBlockHeight = 100,
            EndBlockHeight = 100,
            HasTransaction = true
        };
        List<BlockDto> blockDtos_test7 = new List<BlockDto>();
        try
        {
            blockDtos_test7 =await _blockAppService.GetBlocksAsync(getBlocksInput_test7);
        }catch (Exception e)
        {
            blockDtos_test7 = new List<BlockDto>();
        }

        blockDtos_test7.Count().ShouldBe(0);
        
        //Unit Test 8
        GetBlocksInput getBlocksInput_test8 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 200,
            IsOnlyConfirmed = true,
            HasTransaction = true
        };
        List<BlockDto> blockDtos_test8 =await _blockAppService.GetBlocksAsync(getBlocksInput_test8);
        blockDtos_test8.Count.ShouldBe(1);
        blockDtos_test8[0].BlockHash.ShouldBe(block_180.BlockHash);
        
        //Unit Test 9
        GetBlocksInput getBlocksInput_test9 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 200,
            IsOnlyConfirmed = false,
            HasTransaction = true
        };
        List<BlockDto> blockDtos_test9 =await _blockAppService.GetBlocksAsync(getBlocksInput_test9);
        blockDtos_test9.Count.ShouldBe(4);
        blockDtos_test9.ShouldContain(x=>x.BlockHeight==100);
        blockDtos_test9.ShouldContain(x=>x.BlockHeight==200);
        blockDtos_test9.ShouldContain(x => x.BlockHash == block_180_fork.BlockHash);
        blockDtos_test9.ShouldContain(x => x.BlockHash == block_180.BlockHash);
        blockDtos_test9.Count(x=>x.BlockHeight==180).ShouldBe(2);
        
        //Unit Test 10
        var block_110 = MockDataHelper.MockNewBlockEtoData(110, MockDataHelper.CreateBlockHash(), true);
        // var transaction_110 = MockDataHelper.MockTransactionWithLogEventData(110, block_110.BlockHash,
        //     MockDataHelper.CreateBlockHash(), true, "consensus_contract_address", "");
        // block_110.Transactions = new List<Transaction>() { transaction_110 };
        await _blockIndexRepository.AddAsync(block_110);
        Thread.Sleep(2000);
        GetBlocksInput getBlocksInput_test10 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = true,
            HasTransaction = true,
            Events = new List<FilterContractEventInput>()
            {
                new FilterContractEventInput()
                {
                    ContractAddress = "consensus_contract_address"
                },
                new FilterContractEventInput()
                {
                    ContractAddress = "token_contract_address"
                }
            }
        };
        List<BlockDto> blockDtos_test10 =await _blockAppService.GetBlocksAsync(getBlocksInput_test10);
        blockDtos_test10.Count.ShouldBe(1);
        blockDtos_test10.ShouldContain(x=>x.BlockHeight==110);
        
        //Unit Test 11
        var block_105 = MockDataHelper.MockNewBlockEtoData(105, MockDataHelper.CreateBlockHash(), true);
        // var transaction_105 = MockDataHelper.MockTransactionWithLogEventData(105, block_105.BlockHash,
        //     MockDataHelper.CreateBlockHash(), true, "contract_address_a", "UpdateTinyBlockInformation");
        // block_105.Transactions = new List<Transaction>() { transaction_105 };
        var block_106 = MockDataHelper.MockNewBlockEtoData(106, MockDataHelper.CreateBlockHash(), true);
        // var transaction_106 = MockDataHelper.MockTransactionWithLogEventData(106,block_106.BlockHash, MockDataHelper.CreateBlockHash(),
        //     true, "token_contract_address", "DonateResourceToken");
        // block_106.Transactions = new List<Transaction>() { transaction_106 };
        await _blockIndexRepository.AddAsync(block_105);
        await _blockIndexRepository.AddAsync(block_106);
        Thread.Sleep(2000);
        GetBlocksInput getBlocksInput_test11 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = true,
            HasTransaction = true,
            // Events = new List<FilterContractEventInput>()
            // {
            //     new FilterContractEventInput()
            //     {
            //         EventNames = new List<string>(){"UpdateValue","UpdateTinyBlockInformation"}
            //     },
            //     new FilterContractEventInput()
            //     {
            //         EventNames = new List<string>(){"DonateResourceToken"}
            //     }
            // }
        };
        List<BlockDto> blockDtos_test11 =await _blockAppService.GetBlocksAsync(getBlocksInput_test11);
        // blockDtos_test11.Count.ShouldBe(2);
        blockDtos_test11.ShouldContain(x=>x.BlockHeight==105);
        blockDtos_test11.ShouldContain(x=>x.BlockHeight==106);
        
        //Unit Test 12
        var block_107 = MockDataHelper.MockNewBlockEtoData(107, MockDataHelper.CreateBlockHash(), true);
        // var transaction_107 = MockDataHelper.MockTransactionWithLogEventData(107, block_107.BlockHash,
        //     MockDataHelper.CreateBlockHash(), true, "consensus_contract_address", "UpdateTinyBlockInformation");
        // block_107.Transactions = new List<Transaction>() { transaction_107 };
        await _blockIndexRepository.AddAsync(block_107);
        Thread.Sleep(2000);
        GetBlocksInput getBlocksInput_test12 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = true,
            HasTransaction = true,
            // Events = new List<FilterContractEventInput>()
            // {
            //     new FilterContractEventInput()
            //     {
            //         ContractAddress = "consensus_contract_address",
            //         EventNames = new List<string>(){"UpdateValue","UpdateTinyBlockInformation"}
            //     },
            //     new FilterContractEventInput()
            //     {
            //         ContractAddress = "token_contract_address",
            //         EventNames = new List<string>(){"DonateResourceToken"}
            //     }
            // }
        };
        List<BlockDto> blockDtos_test12 =await _blockAppService.GetBlocksAsync(getBlocksInput_test12);
        // blockDtos_test12.Count.ShouldBe(2);
        blockDtos_test12.ShouldContain(x=>x.BlockHash==block_106.BlockHash);
        blockDtos_test12.ShouldContain(x=>x.BlockHash==block_107.BlockHash);
        // blockDtos_test12.ShouldNotContain(x=>x.BlockHash==block_105.BlockHash);
        
        //Unit Test 13
        GetBlocksInput getBlocksInput_test13 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = true,
            HasTransaction = true,
            // Events = new List<FilterContractEventInput>()
            // {
            //     new FilterContractEventInput()
            //     {
            //         ContractAddress = "consensus_contract_address",
            //         EventNames = new List<string>(){"DonateResourceToken"}
            //     }
            // }
        };
        List<BlockDto> blockDtos_test13 =await _blockAppService.GetBlocksAsync(getBlocksInput_test13);
        // blockDtos_test13.Count.ShouldBe(0);
        
        //Unit Test 30
        GetBlocksInput getBlocksInput_test30 = new GetBlocksInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 200,
            IsOnlyConfirmed = true,
            HasTransaction = true,
            // Events = new List<FilterContractEventInput>()
            // {
            //     new FilterContractEventInput()
            //     {
            //         ContractAddress = "consensus_contract_address",
            //         EventNames = new List<string>(){"UpdateTinyBlockInformation"}
            //     }
            // }
        };
        List<BlockDto> blockDtos_test30 =await _blockAppService.GetBlocksAsync(getBlocksInput_test30);
        // blockDtos_test30.Count.ShouldBe(1);
        blockDtos_test30.ShouldContain(x=>x.BlockHash==block_107.BlockHash);
        await ClearBlockIndex("AELF", 100, 300);
        await ClearBlockIndex("AELF", 1000, 5000);
        await ClearBlockIndex("AELF", 0, 99999);
    }

    private async Task ClearTransactionIndex(string chainId,long startBlockNumber,long endBlockNumber)
    {
        Expression<Func<TransactionIndex, bool>> expression = p => p.ChainId == chainId && p.BlockHeight >= startBlockNumber && p.BlockHeight <= endBlockNumber;
        var queryable = await _transactionIndexRepository.GetQueryableAsync();
        var filterList= queryable.Where(expression).ToList();
        foreach (var deleteTransaction in filterList)
        {
            await _transactionIndexRepository.DeleteAsync(deleteTransaction);
        }
    }
    
    [Fact]
    public async Task GetTransactionAsync_Test14_20_32()
    {
        //clear data for unit test
        ClearTransactionIndex("AELF", 100, 110);

        Thread.Sleep(2000);
        //Unit Test 14
        var transaction_100_1 = MockDataHelper.MockNewTransactionEtoData(100, false, "token_contract_address", "DonateResourceToken");
        var transaction_100_2 = MockDataHelper.MockNewTransactionEtoData(100, false, "", "");
        var transaction_100_3 = MockDataHelper.MockNewTransactionEtoData(100, false, "consensus_contract_address", "UpdateValue");
        var transaction_110 = MockDataHelper.MockNewTransactionEtoData(110, true, "consensus_contract_address", "UpdateTinyBlockInformation");
        await _transactionIndexRepository.AddAsync(transaction_100_1);
        await _transactionIndexRepository.AddAsync(transaction_100_2);
        await _transactionIndexRepository.AddAsync(transaction_100_3);
        await _transactionIndexRepository.AddAsync(transaction_110);
        Thread.Sleep(2000);
        GetTransactionsInput getTransactionsInput_test14 = new GetTransactionsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110
        };
        List<TransactionDto> transactionDtos_test14 =
            await _blockAppService.GetTransactionsAsync(getTransactionsInput_test14);
        transactionDtos_test14.Count().ShouldBe(4);
        
        //Unit Test 15
        GetTransactionsInput getTransactionsInput_test15 = new GetTransactionsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false,
            Events = new List<FilterContractEventInput>()
            {
                new FilterContractEventInput()
                {
                    ContractAddress = "consensus_contract_address",
                },
                new FilterContractEventInput()
                {
                    ContractAddress = "token_contract_address",
                }
            }
        };
        List<TransactionDto> transactionDtos_test15 =
            await _blockAppService.GetTransactionsAsync(getTransactionsInput_test15);
        transactionDtos_test15.Count.ShouldBe(3);
        transactionDtos_test15.ShouldContain(x=>x.TransactionId==transaction_100_1.TransactionId);
        transactionDtos_test15.ShouldContain(x=>x.TransactionId==transaction_100_3.TransactionId);
        transactionDtos_test15.ShouldContain(x=>x.TransactionId==transaction_110.TransactionId);
        
        //Unit Test 16
        GetTransactionsInput getTransactionsInput_test16 = new GetTransactionsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false,
            Events = new List<FilterContractEventInput>()
            {
                new FilterContractEventInput()
                {
                    ContractAddress = "",
                    EventNames = new List<string>(){"UpdateValue","UpdateTinyBlockInformation"}
                },
                new FilterContractEventInput()
                {
                    ContractAddress = "",
                    EventNames = new List<string>(){"DonateResourceToken"}
                }
            }
        };
        List<TransactionDto> transactionDtos_test16 =await _blockAppService.GetTransactionsAsync(getTransactionsInput_test16);
        transactionDtos_test16.Count.ShouldBe(3);
        transactionDtos_test16.ShouldContain(x=>x.TransactionId==transaction_100_1.TransactionId);
        transactionDtos_test16.ShouldContain(x=>x.TransactionId==transaction_100_3.TransactionId);
        transactionDtos_test16.ShouldContain(x=>x.TransactionId==transaction_110.TransactionId);
        
        //Unit Test 17
        GetTransactionsInput getTransactionsInput_test17 = new GetTransactionsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false,
            Events = new List<FilterContractEventInput>()
            {
                new FilterContractEventInput()
                {
                    ContractAddress = "consensus_contract_address",
                    EventNames = new List<string>(){"UpdateValue","UpdateTinyBlockInformation"}
                },
                new FilterContractEventInput()
                {
                    ContractAddress = "token_contract_address",
                    EventNames = new List<string>(){"DonateResourceToken"}
                }
            }
        };
        List<TransactionDto> transactionDtos_test17 =await _blockAppService.GetTransactionsAsync(getTransactionsInput_test17);
        transactionDtos_test17.Count.ShouldBe(3);
        transactionDtos_test17.ShouldContain(x=>x.TransactionId==transaction_100_1.TransactionId);
        transactionDtos_test17.ShouldContain(x=>x.TransactionId==transaction_100_3.TransactionId);
        transactionDtos_test17.ShouldContain(x=>x.TransactionId==transaction_110.TransactionId);
        
        //Unit Test 18
        GetTransactionsInput getTransactionsInput_test18 = new GetTransactionsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false,
            Events = new List<FilterContractEventInput>()
            {
                new FilterContractEventInput()
                {
                    ContractAddress = "consensus_contract_address",
                    EventNames = new List<string>(){"DonateResourceToken"}
                }
            }
        };
        List<TransactionDto> transactionDtos_test18 =await _blockAppService.GetTransactionsAsync(getTransactionsInput_test18);
        transactionDtos_test18.Count.ShouldBe(0);
        
        //Unit Test 19
        GetTransactionsInput getTransactionsInput_test19 = new GetTransactionsInput()
        {
            ChainId = "AELG",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false
        };
        List<TransactionDto> transactionDtos_test19 =await _blockAppService.GetTransactionsAsync(getTransactionsInput_test19);
        transactionDtos_test19.Count.ShouldBe(0);
        
        //Unit Test 20
        GetTransactionsInput getTransactionsInput_test20 = new GetTransactionsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = true
        };
        List<TransactionDto> transactionDtos_test20 =await _blockAppService.GetTransactionsAsync(getTransactionsInput_test20);
        transactionDtos_test20.Count.ShouldBe(1);
        
        //Unit Test 32
        GetTransactionsInput getTransactionsInput_test32 = new GetTransactionsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 200,
            IsOnlyConfirmed = true,
            Events = new List<FilterContractEventInput>()
            {
                new FilterContractEventInput()
                {
                    ContractAddress = "consensus_contract_address",
                    EventNames = new List<string>(){"UpdateTinyBlockInformation"}
                }
            }
        };
        List<TransactionDto> transactionDtos_test32 =await _blockAppService.GetTransactionsAsync(getTransactionsInput_test32);
        transactionDtos_test32.Count.ShouldBe(1);
        transactionDtos_test32.ShouldContain(x=>x.TransactionId==transaction_110.TransactionId);
    }

    [Fact]
    public async Task GetTransactionAsync_Test31()
    {
        await ClearTransactionIndex("AELF", 1000, 5000);
        
        //Unit Test 31
        var transaction_1000 = MockDataHelper.MockNewTransactionEtoData(1000, false,"","");
        var transaction_1500 = MockDataHelper.MockNewTransactionEtoData(1500, false,"","");
        var transaction_1999 = MockDataHelper.MockNewTransactionEtoData(1999, false,"","");
        var transaction_2000 = MockDataHelper.MockNewTransactionEtoData(2000, false,"","");
        var transaction_3000 = MockDataHelper.MockNewTransactionEtoData(3000, false,"","");
        var transaction_4000 = MockDataHelper.MockNewTransactionEtoData(4000, false,"","");
        var transaction_4999 = MockDataHelper.MockNewTransactionEtoData(4999, false,"","");
        var transaction_5000 = MockDataHelper.MockNewTransactionEtoData(5000, false,"","");
        await _transactionIndexRepository.AddAsync(transaction_1000);
        await _transactionIndexRepository.AddAsync(transaction_1500);
        await _transactionIndexRepository.AddAsync(transaction_1999);
        await _transactionIndexRepository.AddAsync(transaction_2000);
        await _transactionIndexRepository.AddAsync(transaction_3000);
        await _transactionIndexRepository.AddAsync(transaction_4000);
        await _transactionIndexRepository.AddAsync(transaction_4999);
        await _transactionIndexRepository.AddAsync(transaction_5000);
        Thread.Sleep(2000);
        GetTransactionsInput getTransactionsInput_test31 = new GetTransactionsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 1000,
            EndBlockHeight = 5000
        };
        List<TransactionDto> getBlocksInput_test31 =await _blockAppService.GetTransactionsAsync(getTransactionsInput_test31);
        getBlocksInput_test31.Max(x=>x.BlockHeight).ShouldBe(1999);
    }
    
    private async Task ClearLogEventIndex(string chainId,long startBlockNumber,long endBlockNumber)
    {
        Expression<Func<LogEventIndex, bool>> expression = p => p.ChainId == chainId && p.BlockHeight >= startBlockNumber && p.BlockHeight <= endBlockNumber;
        var queryable = await _logEventIndexRepository.GetQueryableAsync();
        var filterList= queryable.Where(expression).ToList();
        foreach (var deleteLogEvent in filterList)
        {
            await _logEventIndexRepository.DeleteAsync(deleteLogEvent);
        }
    }
    
    [Fact]
    public async Task GetLogEventAsync_Test21_29()
    {
        //clear data for unit test
        await ClearLogEventIndex("AELF", 0, 99999);

        Thread.Sleep(2000);
        
        //Unit Test 21
        GetLogEventsInput getLogEventsInput_test21 = new GetLogEventsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110
        };
        List<LogEventDto> logEventDtos_test21 =await _blockAppService.GetLogEventsAsync(getLogEventsInput_test21);
        logEventDtos_test21.Count.ShouldBe(0);
        
        //Unit Test 22
        string transactionId_105 = MockDataHelper.CreateBlockHash();
        var logEvent_105_1 = MockDataHelper.MockNewLogEventEtoData(105, transactionId_105, 0, false,
            "token_contract_address", "DonateResourceToken");
        var logEvent_105_2 = MockDataHelper.MockNewLogEventEtoData(105, transactionId_105, 1, false,
            "consensus_contract_address", "UpdateValue");
        await _logEventIndexRepository.AddAsync(logEvent_105_1);
        await _logEventIndexRepository.AddAsync(logEvent_105_2);
        Thread.Sleep(2000);
        GetLogEventsInput getLogEventsInput_test22 = new GetLogEventsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false
        };
        List<LogEventDto> logEventDtos_test22 =await _blockAppService.GetLogEventsAsync(getLogEventsInput_test22);
        logEventDtos_test22.Count.ShouldBe(2);
        
        //Unit Test 23
        var logEvent_106 = MockDataHelper.MockNewLogEventEtoData(106, MockDataHelper.CreateBlockHash(), 0, false,
            "consensus_contract_address", "UpdateTinyBlockInformation");
        await _logEventIndexRepository.AddAsync(logEvent_106);
        Thread.Sleep(2000);
        GetLogEventsInput getLogEventsInput_test23 = new GetLogEventsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false
        };
        List<LogEventDto> logEventDtos_test23 =await _blockAppService.GetLogEventsAsync(getLogEventsInput_test23);
        logEventDtos_test23.Count.ShouldBe(3);
        logEventDtos_test23.ShouldContain(x=>x.BlockHeight==105);
        logEventDtos_test23.ShouldContain(x=>x.BlockHeight==106);
        
        //Unit Test 24
        GetLogEventsInput getLogEventsInput_test24 = new GetLogEventsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false,
            Events = new List<FilterContractEventInput>()
            {
                new FilterContractEventInput()
                {
                    ContractAddress = "consensus_contract_address"
                },
                new FilterContractEventInput()
                {
                    ContractAddress = "token_contract_address"
                }
            }
        };
        List<LogEventDto> logEventDtos_test24 =await _blockAppService.GetLogEventsAsync(getLogEventsInput_test24);
        logEventDtos_test24.Count.ShouldBe(3);
        logEventDtos_test24.ShouldContain(x=>x.EventName==logEvent_105_1.EventName);
        logEventDtos_test24.ShouldContain(x=>x.EventName==logEvent_105_2.EventName);
        logEventDtos_test24.ShouldContain(x=>x.EventName==logEvent_106.EventName);
        
        //Unit Test 25
        var logEvent_107 = MockDataHelper.MockNewLogEventEtoData(107, MockDataHelper.CreateBlockHash(), 0, false,
            "multitoken_contract_address", "Approve");
        var logEvent_108 = MockDataHelper.MockNewLogEventEtoData(108, MockDataHelper.CreateBlockHash(), 0, true,
            "Iptoken_contract_address", "Approve");
        await _logEventIndexRepository.AddAsync(logEvent_107);
        await _logEventIndexRepository.AddAsync(logEvent_108);
        Thread.Sleep(2000);
        GetLogEventsInput getLogEventsInput_test25 = new GetLogEventsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false,
            Events = new List<FilterContractEventInput>()
            {
                new FilterContractEventInput()
                {
                    ContractAddress = "",
                    EventNames = new List<string>(){"UpdateValue","UpdateTinyBlockInformation"}
                },
                new FilterContractEventInput()
                {
                    ContractAddress = "",
                    EventNames = new List<string>(){"DonateResourceToken"}
                },
                new FilterContractEventInput()
                {
                    ContractAddress = "",
                    EventNames = new List<string>(){"Approve"}
                }
            }
        };
        List<LogEventDto> logEventDtos_test25 =await _blockAppService.GetLogEventsAsync(getLogEventsInput_test25);
        logEventDtos_test25.Count.ShouldBe(5);
        logEventDtos_test25.ShouldContain(x=>x.BlockHeight==105);
        logEventDtos_test25.ShouldContain(x=>x.BlockHeight==106);
        logEventDtos_test25.ShouldContain(x=>x.BlockHeight==107);
        logEventDtos_test25.ShouldContain(x=>x.BlockHeight==108);
        
        //Unit Test 26
        var logEvent_104 = MockDataHelper.MockNewLogEventEtoData(104, MockDataHelper.CreateBlockHash(), 0, false,
            "token_contract_address", "DonateResourceToken");
        await _logEventIndexRepository.AddAsync(logEvent_104);
        Thread.Sleep(2000);
        GetLogEventsInput getLogEventsInput_test26 = new GetLogEventsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false,
            Events = new List<FilterContractEventInput>()
            {
                new FilterContractEventInput()
                {
                    ContractAddress = "consensus_contract_address",
                    EventNames = new List<string>(){"UpdateValue","UpdateTinyBlockInformation"}
                },
                new FilterContractEventInput()
                {
                    ContractAddress = "token_contract_address",
                    EventNames = new List<string>(){"DonateResourceToken"}
                }
            }
        };
        List<LogEventDto> logEventDtos_test26 =await _blockAppService.GetLogEventsAsync(getLogEventsInput_test26);
        logEventDtos_test26.Count.ShouldBe(4);
        logEventDtos_test26.ShouldContain(x=>x.BlockHeight==104);
        logEventDtos_test26.ShouldContain(x=>x.BlockHeight==105);
        logEventDtos_test26.ShouldContain(x=>x.BlockHeight==106);
        
        //Unit Test 27
        GetLogEventsInput getLogEventsInput_test27 = new GetLogEventsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false,
            Events = new List<FilterContractEventInput>()
            {
                new FilterContractEventInput()
                {
                    ContractAddress = "consensus_contract_address",
                    EventNames = new List<string>(){"DonateResourceToken"}
                }
            }
        };
        List<LogEventDto> logEventDtos_test27 =await _blockAppService.GetLogEventsAsync(getLogEventsInput_test27);
        logEventDtos_test27.Count.ShouldBe(0);
        
        //Unit Test 28
        GetLogEventsInput getLogEventsInput_test28 = new GetLogEventsInput()
        {
            ChainId = "AELG",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = false
        };
        List<LogEventDto> logEventDtos_test28 = new List<LogEventDto>();
        try
        {
            logEventDtos_test28  =await _blockAppService.GetLogEventsAsync(getLogEventsInput_test28);
        }catch(Exception e)
        {
            logEventDtos_test28 = new List<LogEventDto>();
        }
        logEventDtos_test28.Count.ShouldBe(0);
        
        //Unit Test 29
        GetLogEventsInput getLogEventsInput_test29 = new GetLogEventsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 100,
            EndBlockHeight = 110,
            IsOnlyConfirmed = true
        };
        List<LogEventDto> logEventDtos_test29 =await _blockAppService.GetLogEventsAsync(getLogEventsInput_test29);
        logEventDtos_test29.Count.ShouldBe(1);
        logEventDtos_test29.ShouldContain(x=>x.BlockHeight==108);
        await ClearLogEventIndex("AELF", 0, 99999);


    }

    [Fact]
    public async Task GetLogEventAsync_Test33()
    {
        await ClearBlockIndex("AELF", 0, 99999);
        //Unit Test 33
        var logEvent_1000 = MockDataHelper.MockNewLogEventEtoData(1000,  MockDataHelper.CreateBlockHash(),0,false,"","");
        var logEvent_1500 = MockDataHelper.MockNewLogEventEtoData(1500, MockDataHelper.CreateBlockHash(),0,false,"","");
        var logEvent_1999 = MockDataHelper.MockNewLogEventEtoData(1999, MockDataHelper.CreateBlockHash(),0,false,"","");
        var logEvent_2000 = MockDataHelper.MockNewLogEventEtoData(2000, MockDataHelper.CreateBlockHash(),0,false,"","");
        var logEvent_3000 = MockDataHelper.MockNewLogEventEtoData(3000, MockDataHelper.CreateBlockHash(),0,false,"","");
        var logEvent_4000 = MockDataHelper.MockNewLogEventEtoData(4000, MockDataHelper.CreateBlockHash(),0,false,"","");
        var logEvent_4999 = MockDataHelper.MockNewLogEventEtoData(4999, MockDataHelper.CreateBlockHash(),0,false,"","");
        var logEvent_5000 = MockDataHelper.MockNewLogEventEtoData(5000, MockDataHelper.CreateBlockHash(),0,false,"","");
        await _logEventIndexRepository.AddAsync(logEvent_1000);
        await _logEventIndexRepository.AddAsync(logEvent_1500);
        await _logEventIndexRepository.AddAsync(logEvent_1999);
        await _logEventIndexRepository.AddAsync(logEvent_2000);
        await _logEventIndexRepository.AddAsync(logEvent_3000);
        await _logEventIndexRepository.AddAsync(logEvent_4000);
        await _logEventIndexRepository.AddAsync(logEvent_4999);
        await _logEventIndexRepository.AddAsync(logEvent_5000);
        Thread.Sleep(2000);
        GetLogEventsInput getLogEventsInput_test33 = new GetLogEventsInput()
        {
            ChainId = "AELF",
            StartBlockHeight = 1000,
            EndBlockHeight = 5000
        };
        List<LogEventDto> logEventDtos_test33 =await _blockAppService.GetLogEventsAsync(getLogEventsInput_test33);
        logEventDtos_test33.Max(x=>x.BlockHeight).ShouldBe(1999);
        await ClearBlockIndex("AELF", 0, 99999);
    }

    [Fact]
    public async Task GetBlockCountTest()
    {
        await ClearBlockIndex("AELF", 0, 99999);
        for (int i = 0; i < 20; i++)
        {
            var block = MockDataHelper.MockNewBlockEtoData(100+i, MockDataHelper.CreateBlockHash(),true);
            await _blockIndexRepository.AddAsync(block);
            block = MockDataHelper.MockNewBlockEtoData(120+i, MockDataHelper.CreateBlockHash(),false);
            await _blockIndexRepository.AddAsync(block);
        }
        Thread.Sleep(1000);
        var count = await _blockAppService.GetBlockCountAsync(new GetBlocksInput
        {
            ChainId = "AELF",
            StartBlockHeight = 110,
            EndBlockHeight = 129,
            IsOnlyConfirmed = false
        });
        count.ShouldBe(20);
        
        count = await _blockAppService.GetBlockCountAsync(new GetBlocksInput
        {
            ChainId = "AELF",
            StartBlockHeight = 110,
            EndBlockHeight = 129,
            IsOnlyConfirmed = true
        });
        count.ShouldBe(10);
        await ClearBlockIndex("AELF", 0, 99999);
    }
}