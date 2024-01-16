using AeFinder.Client.GraphQL;
using GraphQL.Types;

namespace GraphQL;

public class TestSchema : AeFinderClientSchema<Query>
{
    public TestSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Mutation = new AutoRegisteringObjectGraphType<TestMutation>();
    }
}