namespace AeFinder.RequestProxy;

public class ProxyOptions
{
    public string[] Urls { get; set; }
    
    public string[] BlockedPathPrefixes { get; set; }
}