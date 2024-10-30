using System.Collections.Generic;

namespace AeFinder.Options;

public class ChainOptions
{
    public Dictionary<string, ChainInfo> ChainInfos { get; set; }
}

public class ChainInfo
{
    public string ChainId { get; set; }
    public string AElfNodeBaseUrl { get; set; }
    public string CAContractAddress { get; set; }
}