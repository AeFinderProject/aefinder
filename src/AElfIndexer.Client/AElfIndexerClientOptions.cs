namespace AElfIndexer.Client;

public class AElfIndexerClientOptions
{
    public AElfIndexerClientType ClientType { get; set; } = AElfIndexerClientType.Full;
}

public enum AElfIndexerClientType
{
    Full, // Provide data processing and query capabilities
    Query // Provide only query capability
}