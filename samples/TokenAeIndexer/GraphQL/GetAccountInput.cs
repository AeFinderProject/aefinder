namespace TokenAeIndexer.GraphQL;

public class GetAccountInput
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
}