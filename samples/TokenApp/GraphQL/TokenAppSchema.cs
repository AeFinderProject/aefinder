using AeFinder.Sdk;

namespace TokenApp.GraphQL;

public class TokenAppSchema : AppSchema<TokenAppQuery>
{
    public TokenAppSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}