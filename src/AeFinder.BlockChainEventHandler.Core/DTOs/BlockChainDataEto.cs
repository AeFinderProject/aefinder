using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace AeFinder.BlockChainEventHandler.DTOs;

[EventName("AElf.WebApp.MessageQueue.BlockChainDataEto")]
public class BlockChainDataEto
{
    // [JsonProperty("chainId")]
    public string ChainId { get; set; }
    public List<BlockEto> Blocks { get; set; }
}