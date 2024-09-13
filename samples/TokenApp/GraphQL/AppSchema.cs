using AeFinder.Sdk;

namespace TokenApp.GraphQL;

public class AppSchema : AppSchema<Query>
{
    public AppSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}