using Newtonsoft.Json;

namespace AElfScan.AElf.ETOs;

public class BlockChainDataEto
{
    // [JsonProperty("chainId")]
    public string ChainId { get; set; }
    public List<BlockEto> Blocks { get; set; }
}