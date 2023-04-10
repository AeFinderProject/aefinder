using AElfIndexer.Client.GraphQL;
using GraphQL.Types;

namespace GraphQL;

public class TestSchema : AElfIndexerClientSchema<Query>
{
    public TestSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Mutation = new AutoRegisteringObjectGraphType<TestMutation>();
    }
}