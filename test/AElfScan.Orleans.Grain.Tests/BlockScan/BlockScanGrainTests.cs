using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScan.AElf.Dtos;
using AElfScan.Orleans.EventSourcing.Grain.BlockScan;
using AElfScan.Orleans.EventSourcing.Grain.Chains;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Streams;
using Shouldly;
using Xunit;

namespace AElfScan.BlockScan;

[Collection(ClusterCollection.Name)]
public class BlockScanGrainTests : AElfScanGrainTestBase
{
    [Fact]
    public async Task BlockTest()
    {
        var chainId = "AELF";
        var clientId = "DApp";
        var version = Guid.NewGuid().ToString();
        var id = chainId + clientId;
        
        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync("hash150", 150);
        await chainGrain.SetLatestConfirmBlockAsync("hash120", 130);

        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(id);
        await clientGrain.InitializeAsync(chainId, clientId, version, new SubscribeInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = false,
            StartBlockNumber = 101
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfScanApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfScanApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
            {
                v.ChainId.ShouldBe(chainId);
                v.ClientId.ShouldBe(clientId);
                v.Version.ShouldBe(version);
                subscribedBlock.AddRange(v.Blocks);
           return Task.CompletedTask;
        });

        await scanGrain.HandleNewBlockAsync(new BlockDto());
        subscribedBlock.Count.ShouldBe(0);

        await scanGrain.HandleHistoricalBlockAsync();
        
        subscribedBlock.Count.ShouldBe(50);
        subscribedBlock.Count(o => o.IsConfirmed).ShouldBe(25);

        var clientInfo = await clientGrain.GetClientInfoAsync();
        clientInfo.ScanModeInfo.ScanMode.ShouldBe(ScanMode.NewBlock);
        clientInfo.ScanModeInfo.ScanNewBlockStartHeight.ShouldBe(126);
        
        await scanGrain.HandleHistoricalBlockAsync();
        subscribedBlock.Count.ShouldBe(50);
        
        var block127 = new BlockDto
        {
            BlockHash = Guid.NewGuid().ToString(),
            BlockNumber = 127,
        };
        await scanGrain.HandleNewBlockAsync(block127);

        subscribedBlock.Count.ShouldBe(52);
        subscribedBlock.Last().BlockNumber.ShouldBe(127);

        var block126Hash = subscribedBlock.Last().PreviousBlockHash;
        var block127Hash = subscribedBlock.Last().BlockHash;
        
        var block128 = new BlockDto
        {
            BlockHash = Guid.NewGuid().ToString(),
            BlockNumber = 128,
            PreviousBlockHash = block127Hash
        };
        await scanGrain.HandleNewBlockAsync(block128);
        
        subscribedBlock.Count.ShouldBe(53);
        subscribedBlock.Last().BlockNumber.ShouldBe(128);
        
        var block128New = new BlockDto
        {
            BlockHash = Guid.NewGuid().ToString(),
            BlockNumber = 128,
            PreviousBlockHash = block127Hash
        };
        await scanGrain.HandleNewBlockAsync(block128New);
        
        subscribedBlock.Count.ShouldBe(54);
        subscribedBlock.Last().BlockNumber.ShouldBe(128);
        
        var block127New = new BlockDto
        {
            BlockHash = Guid.NewGuid().ToString(),
            BlockNumber = 127,
            PreviousBlockHash = Guid.NewGuid().ToString()
        };
        await scanGrain.HandleNewBlockAsync(block127New);
        
        subscribedBlock.Count.ShouldBe(56);
        subscribedBlock.Last().BlockNumber.ShouldBe(127);
        
        await scanGrain.HandleConfirmedBlockAsync(new List<BlockDto>
        {
            new BlockDto
            {
                BlockHash = block126Hash,
                BlockNumber = 126,
            }
        });
        subscribedBlock.Count.ShouldBe(57);
        subscribedBlock.Last().BlockNumber.ShouldBe(126);
        
        await scanGrain.HandleConfirmedBlockAsync(new List<BlockDto>
        {
            block128
        });
        subscribedBlock.Count.ShouldBe(57);
        subscribedBlock.Last().BlockNumber.ShouldBe(126);
    }
    
    [Fact]
    public async Task OnlyConfirmedBlockTest()
    {
        var chainId = "AELF";
        var clientId = "DApp";
        var version = Guid.NewGuid().ToString();
        var id = chainId + clientId;
        
        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync("hash150", 150);
        await chainGrain.SetLatestConfirmBlockAsync("hash120", 130);

        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(id);
        await clientGrain.InitializeAsync(chainId, clientId, version, new SubscribeInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = true,
            StartBlockNumber = 101
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client 
                .GetStreamProvider(AElfScanApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfScanApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.ClientId.ShouldBe(clientId);
            v.Version.ShouldBe(version);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await scanGrain.HandleHistoricalBlockAsync();
        
        subscribedBlock.Count.ShouldBe(25);
        var number = 101;
        foreach (var block in subscribedBlock)
        {
            block.BlockNumber.ShouldBe(number);
            block.IsConfirmed.ShouldBe(true);
            number++;
        }
    }
    
    [Fact]
    public async Task EventTest()
    {
        var chainId = "AELF";
        var clientId = "DApp";
        var version = Guid.NewGuid().ToString();
        var id = chainId + clientId;
        
        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync("hash150", 150);
        await chainGrain.SetLatestConfirmBlockAsync("hash120", 130);

        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(id);
        await clientGrain.InitializeAsync(chainId, clientId, version, new SubscribeInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = true,
            StartBlockNumber = 101,
            SubscribeEvents = new List<FilterContractEventInput>
            {
                new FilterContractEventInput
                {
                    ContractAddress = "ContractAddress",
                    EventNames = new List<string>{"EventName"}
                }
            }
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client 
                .GetStreamProvider(AElfScanApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfScanApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.ClientId.ShouldBe(clientId);
            v.Version.ShouldBe(version);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await scanGrain.HandleHistoricalBlockAsync();
        
        subscribedBlock.Count.ShouldBe(25);

        var block126 = new BlockDto
        {
            BlockHash = Guid.NewGuid().ToString(),
            BlockNumber = 126,
            PreviousBlockHash = subscribedBlock.Last().BlockHash,
            Transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    LogEvents = new List<LogEventDto>
                    {
                        new LogEventDto
                        {
                            ContractAddress = "ContractAddress",
                            EventName = "EventName"
                        }
                    }
                }
            }
        };
        await scanGrain.HandleConfirmedBlockAsync(new List<BlockDto> { block126 });
        subscribedBlock.Count.ShouldBe(26);
        
        var block127 = new BlockDto
        {
            BlockHash = Guid.NewGuid().ToString(),
            BlockNumber = 127,
            PreviousBlockHash = block126.BlockHash,
            Transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    LogEvents = new List<LogEventDto>
                    {
                        new LogEventDto
                        {
                            ContractAddress = "ContractAddress",
                            EventName = "EventName2"
                        }
                    }
                }
            }
        };
        await scanGrain.HandleConfirmedBlockAsync(new List<BlockDto> { block127 });
        subscribedBlock.Count.ShouldBe(26);
        
        var block128 = new BlockDto
        {
            BlockHash = Guid.NewGuid().ToString(),
            BlockNumber = 128,
            PreviousBlockHash = block127.BlockHash,
            Transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    LogEvents = new List<LogEventDto>
                    {
                        new LogEventDto
                        {
                            ContractAddress = "ContractAddress2",
                            EventName = "EventName"
                        }
                    }
                }
            }
        };
        await scanGrain.HandleConfirmedBlockAsync(new List<BlockDto> { block128 });
        subscribedBlock.Count.ShouldBe(26);
    }
}