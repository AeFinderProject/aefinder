using GraphQL.Types;

namespace AElfIndexer.Client;

public abstract class AElfIndexerClientQuery : ObjectGraphType
{
    protected AElfIndexerClientQuery()
    {
        Name = "Query";
    }
}