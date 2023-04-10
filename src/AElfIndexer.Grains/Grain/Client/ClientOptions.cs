namespace AElfIndexer.Grains.Grain.Client;

public class ClientOptions
{
    public int MaxCountPerBlockStateSetBucket { get; set; } = 150;
    public int DAppDataCacheCount { get; set; } = 1000;
}