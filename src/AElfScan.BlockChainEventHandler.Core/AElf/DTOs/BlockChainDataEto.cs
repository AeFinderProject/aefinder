using Newtonsoft.Json;

namespace AElfScan.AElf.DTOs;

public class BlockChainDataEto
{
    // [JsonProperty("chainId")]
    public string ChainId { get; set; }
    public List<BlockEto> Blocks { get; set; }
}