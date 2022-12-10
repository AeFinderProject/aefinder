using GraphQL.Types;

namespace AElfIndexer.Client.GraphQL;

public class AElfIndexerClientSchema<TQuery> : Schema
{
    protected AElfIndexerClientSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query = new AutoRegisteringObjectGraphType<TQuery>();
    }
}