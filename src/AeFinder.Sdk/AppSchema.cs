using GraphQL.Types;

namespace AeFinder.Sdk;

public class AppSchema<TQuery>: Schema
{
    protected AppSchema(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        this.Query = (IObjectGraphType) new AutoRegisteringObjectGraphType<TQuery>();
    }
}