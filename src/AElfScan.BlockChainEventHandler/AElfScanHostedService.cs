using System;
using System.Threading;
using System.Threading.Tasks;
using AElfScan.AElf;
using AElfScan.AElf.ETOs;
// using AElfScan.AElf.Dtos;
// using AElfScan.AElf.Etos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;

namespace AElfScan;

public class AElfScanHostedService:IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ILogger<AElfScanHostedService> _logger;

    public AElfScanHostedService(
        IAbpApplicationWithExternalServiceProvider application,
        IDistributedEventBus distributedEventBus,
        ILogger<AElfScanHostedService> logger,
        IServiceProvider serviceProvider)
    {
        _application = application;
        _serviceProvider = serviceProvider;
        _distributedEventBus = distributedEventBus;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _application.Initialize(_serviceProvider);

        // BlockEventDataDto eventData = new BlockEventDataDto()
        // {
        //     BlockNumber = 3007,
        //     IsConfirmed = false
        // };
        // var appService = _serviceProvider.GetRequiredService<AElfTestAppService>();
        // appService.SaveBlock(eventData);
        
        
        _logger.LogInformation("Start publish Event to Rabbitmq");
        _distributedEventBus.PublishAsync(
            new BlockChainDataEto
            {
                ChainId = "AELF",
                Blocks = new List<BlockEto>()
                {
                    new BlockEto()
                    {
                        BlockHash = "fc4192b920415cae740c808e064696890b031944bc2756e18b99619aae50740e",
                        BlockNumber = 336,
                        BlockTime = new DateTime(2022,9,21,15,24,36),
                        PreviousBlockHash = "156ff3721154b30e08bdbd3aab85071c2bc8744dc3249471100c21268bb4641a",
                        Signature = "a41ce21264d62e141537fcdd0597c496969c8ce73ec51dcf4b48411fb66a6134759d2b7cf12c19aa916c72deb8aeba4f20cfbf9deb1a915a52c3ed168c5aa83900",
                        SignerPubkey = "04bcd1c887cd0edbd4ccf8d9d2b3f72e72511aa6183199600313687ba6c583f13c3d6d716fa40df8604aaed0fcab31135fe3c2d45c009800c075254a3782b4c4db",
                        ExtraProperties = new Dictionary<string, string>()
                        {
                            ["Version"]="0",
                            ["Bloom"]="AAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAEAAAAAAAAgAAABAAAAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAgAAAAAAAAAABAAAAAAAAAAAAAAIQAAAAABAAAAAgAAAAAAAAAAAAAAAAABACAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIgAAAAAAAAAAAAAAAAAAAAAAAEAABAAAAAAAAAAAAAAAAAAAAgAAAAgAAAAABAAAAAAAAAAAAAACAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAgAAAAAAAAAAAAAA==AAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAEAAAAAAAAgAAABAAAAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAgAAAAAAAAAABAAAAAAAAAAAAAAIQAAAAABAAAAAgAAAAAAAAAAAAAAAAABACAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIgAAAAAAAAAAAAAAAAAAAAAAAEAABAAAAAAAAAAAAAAAAAAAAgAAAAgAAAAABAAAAAAAAAAAAAACAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAgAAAAAAAAAAAAAA==",
                            ["ExtraData"]="{ \"chainId\": 9992731, \"previousBlockHash\": \"156ff3721154b30e08bdbd3aab85071c2bc8744dc3249471100c21268bb4641a\", \"merkleTreeRootOfTransactions\": \"2818daae4558ecfcf143d2b4e46fcf55edcc2451faa310cc3973237d7374766a\", \"merkleTreeRootOfWorldState\": \"58ae5c52a15f38786b29ded30a70d96e085418c813f01c4398f8f735100a2172\", \"bloom\": \"AAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAEAAAAAAAAgAAABAAAAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAgAAAAAAAAAABAAAAAAAAAAAAAAIQAAAAABAAAAAgAAAAAAAAAAAAAAAAABACAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIgAAAAAAAAAAAAAAAAAAAAAAAEAABAAAAAAAAAAAAAAAAAAAAgAAAAgAAAAABAAAAAAAAAAAAAACAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAgAAAAAAAAAAAAAA==\", \"height\": \"336\", \"extraData\": { \"CrossChain\": \"\", \"Consensus\": \"CkEEvNHIh80O29TM+NnSs/cuclEaphgxmWADE2h7psWD8Tw9bXFvpA34YEqu0PyrMRNf48LUXACYAMB1JUo3grTE2xKGBAgZEvsDCoIBMDRiY2QxYzg4N2NkMGVkYmQ0Y2NmOGQ5ZDJiM2Y3MmU3MjUxMWFhNjE4MzE5OTYwMDMxMzY4N2JhNmM1ODNmMTNjM2Q2ZDcxNmZhNDBkZjg2MDRhYWVkMGZjYWIzMTEzNWZlM2MyZDQ1YzAwOTgwMGMwNzUyNTRhMzc4MmI0YzRkYhLzAggBEAEiIgog2cHgeGZOkxJw2X1PiE2hHXDx06nHQ7rzin6nB+bq46QqIgog1lSTW/6zNSoxlnfDSmEufuqo5f6H64PV90/n0tib0Kw4zwJKggEwNGJjZDFjODg3Y2QwZWRiZDRjY2Y4ZDlkMmIzZjcyZTcyNTExYWE2MTgzMTk5NjAwMzEzNjg3YmE2YzU4M2YxM2MzZDZkNzE2ZmE0MGRmODYwNGFhZWQwZmNhYjMxMTM1ZmUzYzJkNDVjMDA5ODAwYzA3NTI1NGEzNzgyYjRjNGRiUiIKIPGQ8ga2zO3CXXtZkjlMma6CI7VSQFA7ZMYrIU6zGT/uWAFgAWoGCJjI3JgGagsImMjcmAYQ2ILvQmoLCJjI3JgGEICn8H1qDAiYyNyYBhD42Zu5AWoMCJjI3JgGENDErfMBagwImMjcmAYQ+NjarwJqDAiYyNyYBhCwr4zsAmoMCJjI3JgGEKDNirQDagYInMjcmAaAAQmIAdACUJzI3JgG\", \"SystemTransactionCount\": \"CAI=\" }, \"time\": \"2022-09-06T10:42:36Z\", \"merkleTreeRootOfTransactionStatus\": \"317637d50870824e2186aef029745a92b59a598e9cb186d13d7e8b0478342581\", \"signerPubkey\": \"BLzRyIfNDtvUzPjZ0rP3LnJRGqYYMZlgAxNoe6bFg/E8PW1xb6QN+GBKrtD8qzETX+PC1FwAmADAdSVKN4K0xNs=\", \"signature\": \"pBziEmTWLhQVN/zdBZfElpacjOc+xR3PS0hBH7ZqYTR1nSt88SwZqpFsct64rrpPIM+/nesakVpSw+0WjFqoOQA=\" }",
                            ["MerkleTreeRootOfTransactions"]="2818daae4558ecfcf143d2b4e46fcf55edcc2451faa310cc3973237d7374766a",
                            ["MerkleTreeRootOfWorldState"]="58ae5c52a15f38786b29ded30a70d96e085418c813f01c4398f8f735100a2172"
                        },
                        Transactions = new List<TransactionEto>()
                        {
                            new TransactionEto()
                            {
                                TransactionId = "1ce2f46ebfe64f59bef89af5b3f44efb37e4b2ef9e28b28803e960c4a4a400b6",
                                From = "2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e",
                                To="pGa4e5hNGsgkfjEGm72TEvbF7aRDqKBd4LuXtab4ucMbXLcgJ",
                                MethodName = "UpdateValue",
                                Params = "CiIKINnB4HhmTpMScNl9T4hNoR1w8dOpx0O684p+pwfm6uOkEiIKINZUk1v+szUqMZZ3w0phLn7qqOX+h+uD1fdP59LYm9CsIiIKIPGQ8ga2zO3CXXtZkjlMma6CI7VSQFA7ZMYrIU6zGT/uKgYInMjcmAYwAVDPAlqpAQqCATA0YmNkMWM4ODdjZDBlZGJkNGNjZjhkOWQyYjNmNzJlNzI1MTFhYTYxODMxOTk2MDAzMTM2ODdiYTZjNTgzZjEzYzNkNmQ3MTZmYTQwZGY4NjA0YWFlZDBmY2FiMzExMzVmZTNjMmQ0NWMwMDk4MDBjMDc1MjU0YTM3ODJiNGM0ZGISIgog8ZDyBrbM7cJde1mSOUyZroIjtVJAUDtkxishTrMZP+5g0AI=",
                                Signature = "1KblGpvuuo+HSDdh0OhRq/vg3Ts4HoqcIwBeni/356pdEbgnnR2yqbpgvzNs+oNeBb4Ux2kE1XY9lk+p60LfWgA=",
                                Status = 3,
                                ExtraProperties = new Dictionary<string, string>()
                                {
                                    ["Version"]="0",
                                    ["RefBlockNumber"]="335",
                                    ["RefBlockPrefix"]="156ff372",
                                    ["Bloom"]="AAAAAAAAEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAAAAEAAAAAAAAgAAABAAAAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAgAAAAAAAAAABAAAAAAAAAAAAAAIQAAAAABAAAAAgAAAAAAAAAAAAAAAAABACAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIgAAAAAAAAAAAAAAAAAAAAAAAEAABAAAAAAAAAAAAAAAAAAAAgAAAAgAAAAABAAAAAAAAAAAAAACAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAgAAAAAAAAAAAAAA==",
                                    ["ReturnValue"]="",
                                    ["Error"]=""
                                },
                                LogEvents = new List<LogEventEto>()
                                {
                                    new LogEventEto()
                                    {
                                        ContractAddress = "pGa4e5hNGsgkfjEGm72TEvbF7aRDqKBd4LuXtab4ucMbXLcgJ",
                                        EventName = "IrreversibleBlockFound",
                                        Index = 0,
                                        ExtraProperties = new Dictionary<string, string>()
                                        {
                                            ["Indexed"]="[ \"CMAC\" ]",
                                            ["NonIndexed"]=""
                                        }
                                    },
                                    new LogEventEto()
                                    {
                                        ContractAddress = "pGa4e5hNGsgkfjEGm72TEvbF7aRDqKBd4LuXtab4ucMbXLcgJ",
                                        EventName = "MiningInformationUpdated",
                                        Index = 1,
                                        ExtraProperties = new Dictionary<string, string>()
                                        {
                                            ["Indexed"]="[ \"CoIBMDRiY2QxYzg4N2NkMGVkYmQ0Y2NmOGQ5ZDJiM2Y3MmU3MjUxMWFhNjE4MzE5OTYwMDMxMzY4N2JhNmM1ODNmMTNjM2Q2ZDcxNmZhNDBkZjg2MDRhYWVkMGZjYWIzMTEzNWZlM2MyZDQ1YzAwOTgwMGMwNzUyNTRhMzc4MmI0YzRkYg==\", \"EgYInMjcmAY=\", \"GgtVcGRhdGVWYWx1ZQ==\", \"INAC\", \"KiIKIBVv83IRVLMOCL29OquFBxwryHRNwySUcRAMISaLtGQa\" ]",
                                            ["NonIndexed"]=""
                                        }
                                    }
                                }
                            },
                            new TransactionEto()
                            {
                                TransactionId = "5ba449c61035cf8fea16604cf333600b28cebc63f03557634c9531d8229d60c3",
                                From = "2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e",
                                To = "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
                                MethodName = "DonateResourceToken",
                                Params = "EiIKIBVv83IRVLMOCL29OquFBxwryHRNwySUcRAMISaLtGQaGM8C",
                                Signature = "3USrQq3C0VJ28pg1SEA4DJ3vH3suBiW5oFIp53kW7989vdrbgWhCW82qD4ovb6Q9gZOJsqgu388++MMk/3cHDgE=",
                                Status = 3,
                                ExtraProperties = new Dictionary<string, string>()
                                {
                                    ["Version"]="0",
                                    ["RefBlockNumber"]="335",
                                    ["RefBlockPrefix"]="156ff372",
                                    ["Bloom"]="",
                                    ["ReturnValue"]="",
                                    ["Error"]=""
                                },
                                LogEvents = new List<LogEventEto>(){}
                            }
                        }
                    }
                }
            }
            );
        _logger.LogInformation("Test Block Event is already published");
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}