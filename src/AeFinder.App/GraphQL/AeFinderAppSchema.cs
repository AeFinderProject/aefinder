using GraphQL.Types;

namespace AeFinder.App.GraphQL;

public class AeFinderAppSchema<TQuery> : Schema
{
    protected AeFinderAppSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query = new AutoRegisteringObjectGraphType<TQuery>();
    }
}