namespace AeFinder.Options;

public class SignatureGrantOptions
{
    public int TimestampValidityRangeMinutes { get; set; }
    public string LoginChainId { get; set; }
    public string PortkeyV2GraphQLUrl { get; set; }
    public string CommonPrivateKeyForCallTx { get; set; }
}