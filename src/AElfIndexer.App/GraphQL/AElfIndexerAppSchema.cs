using GraphQL.Types;

namespace AElfIndexer.App.GraphQL;

public class AElfIndexerAppSchema<TQuery> : Schema
{
    protected AElfIndexerAppSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query = new AutoRegisteringObjectGraphType<TQuery>();
    }
}