namespace AElfIndexer.Grains.Grain.Client;

public class DappDataValue<T>
{
    public T LatestValue { get; set; }
    
    public T LIBValue { get; set; }
}