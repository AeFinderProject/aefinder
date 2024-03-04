using GraphQL.Types;

namespace AElfIndexer.App.GraphQL;

public class AElfIndexerClientSchema<TQuery> : Schema
{
    protected AElfIndexerClientSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query = new AutoRegisteringObjectGraphType<TQuery>();
    }
}