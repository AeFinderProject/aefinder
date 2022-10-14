namespace AElfScan.Options;

public class OrleansClientOption
{
    public string ClusterId { get; set; }
    public string ServiceId { get; set; }
    /// <summary>
    /// The IP addresses that will be utilized in the cluster.
    /// First IP address is the primary.
    /// </summary>
    public string[] NodeIpAddresses { get; set; }
    /// <summary>
    /// The port used for Client to Server communication.
    /// </summary>
    public int GatewayPort { get; set; }
    /// <summary>
    /// The port for Silo to Silo communication
    /// </summary>
    public int SiloPort { get; set; }
}