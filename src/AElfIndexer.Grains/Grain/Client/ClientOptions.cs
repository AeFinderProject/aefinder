namespace AElfIndexer.Grains.Grain.Client;

public class ClientOptions
{
    public int MaxCountPerBlockStateSetBucket { get; set; } = 200;
    public int DAppDataCacheCount { get; set; } = 1000;
}