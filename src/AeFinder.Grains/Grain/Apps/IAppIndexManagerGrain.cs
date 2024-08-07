namespace AeFinder.Grains.Grain.Apps;

public interface IAppIndexManagerGrain : IGrainWithStringKey
{
    Task AddIndexNameAsync(string indexName);
    Task ClearVersionIndexAsync();
    Task ClearGrainStateAsync();
}