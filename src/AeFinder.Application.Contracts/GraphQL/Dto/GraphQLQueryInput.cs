namespace AeFinder.GraphQL.Dto;

public class GraphQLQueryInput
{
    public string Query { get; set; }
    public object Variables { get; set; }
}