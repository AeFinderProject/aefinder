namespace AElfIndexer.Grains.State.Client;

public class DappDataGrainState<T>
{
    public T LatestValue { get; set; }

    public T LIBValue { get; set; }
}