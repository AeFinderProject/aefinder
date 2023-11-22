using GraphQL.Types;

namespace AElfIndexer.Sdk;

public class ClientSchema<TQuery>: Schema
{
    protected ClientSchema(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        this.Query = (IObjectGraphType) new AutoRegisteringObjectGraphType<TQuery>();
    }
}