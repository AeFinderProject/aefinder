using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace AElfIndexer.Client;

public class AElfIndexerClientSchema<TQuery> : Schema where TQuery : AElfIndexerClientQuery
{
    protected AElfIndexerClientSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query = serviceProvider.GetRequiredService<TQuery>();
    }
}