using Newtonsoft.Json;
using Volo.Abp.EventBus;

namespace AElfScan.AElf.DTOs;

[EventName("AElf.WebApp.MessageQueue.BlockChainDataEto")]
public class BlockChainDataEto
{
    // [JsonProperty("chainId")]
    public string ChainId { get; set; }
    public List<BlockEto> Blocks { get; set; }
}