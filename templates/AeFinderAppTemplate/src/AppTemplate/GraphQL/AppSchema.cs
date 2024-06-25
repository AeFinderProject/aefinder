using AeFinder.Sdk;

namespace AppTemplate.GraphQL;

public class AppSchema : AppSchema<Query>
{
    public AppSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}