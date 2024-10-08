namespace AeFinder.App;

public class AppInfoOptions
{
    public string AppId { get; set; }
    public string Version { get; set; }
    public ClientType ClientType { get; set; } = ClientType.Full;
    public string ChainId { get; set; }
}

public enum ClientType
{
    Full, // Provide data processing and query capabilities
    Query // Provide only query capability
}