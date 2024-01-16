using Xunit;

namespace AeFinder.Grains;

[CollectionDefinition(ClusterCollection.Name)]
public class ClusterCollection:ICollectionFixture<ClusterFixture>
{
    public const string Name = "ClusterCollection";
}