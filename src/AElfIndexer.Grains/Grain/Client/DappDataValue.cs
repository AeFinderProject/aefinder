namespace AElfIndexer.Grains.Grain.Client;

public class DappDataValue
{
    public string LatestValue { get; set; }
    
    public string LIBValue { get; set; }
}

public class DappDataValue<T>
{
    public T LatestValue { get; set; }
    
    public T LIBValue { get; set; }
}