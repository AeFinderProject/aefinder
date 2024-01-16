using GraphQL.Types;

namespace AeFinder.Client.GraphQL;

public class AeFinderClientSchema<TQuery> : Schema
{
    protected AeFinderClientSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query = new AutoRegisteringObjectGraphType<TQuery>();
    }
}