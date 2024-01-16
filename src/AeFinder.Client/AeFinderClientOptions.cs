namespace AeFinder.Client;

public class AeFinderClientOptions
{
    public AeFinderClientType ClientType { get; set; } = AeFinderClientType.Full;
}

public enum AeFinderClientType
{
    Full, // Provide data processing and query capabilities
    Query // Provide only query capability
}